using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// 测试基类，提供通用的测试工具方法
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        // 静态锁对象，确保ConfigManager操作的线程安全
        private static readonly object ConfigManagerLock = new object();
        
        protected readonly string TestTempDirectory;
        protected readonly string TestDataDirectory;

        protected TestBase()
        {
            // 首先重置ConfigManager状态，确保每个测试都从干净状态开始
            ResetConfigManager();
            
            // 重置ServiceContainer状态，确保测试隔离
            try
            {
                var serviceContainerType = typeof(CodeUnfucker.Services.ServiceContainer);
                var resetMethod = serviceContainerType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
                resetMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重置ServiceContainer失败: {ex.Message}");
            }
            
            // 创建临时测试目录
            TestTempDirectory = Path.Combine(Path.GetTempPath(), "CodeUnfucker.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(TestTempDirectory);

            // 设置测试数据目录
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
            TestDataDirectory = Path.Combine(assemblyDir, "TestData");
        }

        /// <summary>
        /// 创建临时文件
        /// </summary>
        protected string CreateTempFile(string fileName, string content)
        {
            var filePath = Path.Combine(TestTempDirectory, fileName);
            var directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// 创建临时配置文件
        /// </summary>
        protected string CreateTempConfigFile<T>(string fileName, T config)
        {
            var configDir = Path.Combine(TestTempDirectory, "Config");
            Directory.CreateDirectory(configDir);
            
            var filePath = Path.Combine(configDir, fileName);
            // 使用与ConfigManager相同的序列化选项
            var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
            File.WriteAllText(filePath, jsonContent);
            return filePath;
        }

        /// <summary>
        /// 读取测试数据文件
        /// </summary>
        protected string ReadTestDataFile(string fileName)
        {
            var filePath = Path.Combine(TestDataDirectory, fileName);
            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        protected void AssertFileExists(string filePath)
        {
            File.Exists(filePath).Should().BeTrue($"文件应该存在: {filePath}");
        }

        /// <summary>
        /// 检查文件内容
        /// </summary>
        protected void AssertFileContent(string filePath, string expectedContent)
        {
            AssertFileExists(filePath);
            var actualContent = File.ReadAllText(filePath);
            actualContent.Should().Be(expectedContent);
        }

        /// <summary>
        /// 检查文件内容包含指定文本
        /// </summary>
        protected void AssertFileContains(string filePath, string expectedText)
        {
            AssertFileExists(filePath);
            var actualContent = File.ReadAllText(filePath);
            actualContent.Should().Contain(expectedText);
        }

        /// <summary>
        /// 为当前测试设置隔离的配置路径，确保不会加载项目的配置文件
        /// </summary>
        protected void SetIsolatedConfigPath()
        {
            var isolatedConfigPath = Path.Combine(TestTempDirectory, "IsolatedConfig");
            ConfigManager.SetConfigPath(isolatedConfigPath);
        }

        /// <summary>
        /// 执行需要配置隔离的操作，确保ConfigManager状态不被其他并发测试影响
        /// </summary>
        protected T ExecuteWithConfigIsolation<T>(Func<T> operation)
        {
            lock (ConfigManagerLock)
            {
                // 在锁内重置ConfigManager状态
                ResetConfigManagerInternal();
                
                try
                {
                    // 执行操作
                    var result = operation();
                    
                    // 再次重置以清理状态
                    ResetConfigManagerInternal();
                    
                    return result;
                }
                catch
                {
                    // 即使发生异常也要重置状态
                    ResetConfigManagerInternal();
                    throw;
                }
            }
        }

        /// <summary>
        /// 执行需要配置隔离的操作，确保ConfigManager状态不被其他并发测试影响
        /// </summary>
        protected void ExecuteWithConfigIsolation(Action operation)
        {
            lock (ConfigManagerLock)
            {
                // 在锁内重置ConfigManager状态
                ResetConfigManagerInternal();
                
                try
                {
                    // 执行操作
                    operation();
                    
                    // 再次重置以清理状态
                    ResetConfigManagerInternal();
                }
                catch
                {
                    // 即使发生异常也要重置状态
                    ResetConfigManagerInternal();
                    throw;
                }
            }
        }

        /// <summary>
        /// 执行需要配置隔离的异步操作，确保ConfigManager状态不被其他并发测试影响
        /// </summary>
        protected async Task ExecuteWithConfigIsolationAsync(Func<Task> operation)
        {
            // 注意：由于异步操作的特性，我们不能在整个异步操作期间持有锁
            // 所以我们需要在操作前后分别重置状态
            lock (ConfigManagerLock)
            {
                ResetConfigManagerInternal();
                // 同时重置ServiceContainer状态
                try
                {
                    var serviceContainerType = typeof(CodeUnfucker.Services.ServiceContainer);
                    var resetMethod = serviceContainerType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
                    resetMethod?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"重置ServiceContainer失败: {ex.Message}");
                }
            }
            
            try
            {
                // 执行异步操作
                await operation();
            }
            finally
            {
                // 即使发生异常也要重置状态
                lock (ConfigManagerLock)
                {
                    ResetConfigManagerInternal();
                    // 同时重置ServiceContainer状态
                    try
                    {
                        var serviceContainerType = typeof(CodeUnfucker.Services.ServiceContainer);
                        var resetMethod = serviceContainerType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
                        resetMethod?.Invoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"重置ServiceContainer失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 执行需要配置隔离的异步操作并返回结果，确保ConfigManager状态不被其他并发测试影响
        /// </summary>
        protected async Task<T> ExecuteWithConfigIsolationAsync<T>(Func<Task<T>> operation)
        {
            // 注意：由于异步操作的特性，我们不能在整个异步操作期间持有锁
            // 所以我们需要在操作前后分别重置状态
            lock (ConfigManagerLock)
            {
                ResetConfigManagerInternal();
            }
            
            try
            {
                // 执行异步操作
                return await operation();
            }
            finally
            {
                // 即使发生异常也要重置状态
                lock (ConfigManagerLock)
                {
                    ResetConfigManagerInternal();
                }
            }
        }

        public virtual void Dispose()
        {
            // 重置ConfigManager的配置路径和缓存
            ResetConfigManager();
            
            // 重置ServiceContainer状态
            try
            {
                var serviceContainerType = typeof(CodeUnfucker.Services.ServiceContainer);
                var resetMethod = serviceContainerType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
                resetMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重置ServiceContainer失败: {ex.Message}");
            }
            
            // 额外确保ConfigManager状态完全重置
            try
            {
                // 设置为一个不存在的路径，确保不会意外加载配置文件
                var configManagerType = typeof(ConfigManager);
                var customConfigPathField = configManagerType.GetField("_customConfigPath", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                var nonExistentPath = Path.Combine(Path.GetTempPath(), "CodeUnfucker_NonExistent_" + Guid.NewGuid());
                customConfigPathField?.SetValue(null, nonExistentPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"额外重置ConfigManager失败: {ex.Message}");
            }
            
            // 清理临时目录
            if (Directory.Exists(TestTempDirectory))
            {
                try
                {
                    Directory.Delete(TestTempDirectory, true);
                }
                catch (Exception ex)
                {
                    // 忽略清理错误，避免影响测试结果
                    Console.WriteLine($"清理临时目录失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 重置ConfigManager状态，确保测试之间不互相影响
        /// </summary>
        protected void ResetConfigManager()
        {
            lock (ConfigManagerLock)
            {
                ResetConfigManagerInternal();
            }
        }

        /// <summary>
        /// 内部重置ConfigManager状态的实现（不带锁）
        /// </summary>
        private void ResetConfigManagerInternal()
        {
            try
            {
                // 使用反射重置ConfigManager的私有静态字段
                var configManagerType = typeof(ConfigManager);
                
                // 重置缓存的配置对象 (确保完全清空)
                var formatterConfigField = configManagerType.GetField("_formatterConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                formatterConfigField?.SetValue(null, null);
                
                var analyzerConfigField = configManagerType.GetField("_analyzerConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                analyzerConfigField?.SetValue(null, null);
                
                var usingRemoverConfigField = configManagerType.GetField("_usingRemoverConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                usingRemoverConfigField?.SetValue(null, null);
                
                // 强制调用ReloadConfigs()方法，确保缓存被清空
                var reloadMethod = configManagerType.GetMethod("ReloadConfigs", BindingFlags.Public | BindingFlags.Static);
                reloadMethod?.Invoke(null, null);
                
                // 设置一个明确不存在的配置路径，确保不会加载项目中的任何配置文件
                var nonExistentPath = Path.Combine(TestTempDirectory, "NonExistentConfig");
                var customConfigPathField = configManagerType.GetField("_customConfigPath", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                customConfigPathField?.SetValue(null, nonExistentPath);
                
                // 再次重置缓存对象确保完全清空
                formatterConfigField?.SetValue(null, null);
                analyzerConfigField?.SetValue(null, null);
                usingRemoverConfigField?.SetValue(null, null);
                
                // 强制垃圾回收，确保没有残留的对象引用
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect(); // 再次调用确保完全清理
                
                // 添加小延迟确保所有清理操作完成
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重置ConfigManager失败: {ex.Message}");
            }
        }
    }
} 