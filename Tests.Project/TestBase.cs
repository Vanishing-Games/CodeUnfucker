using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// 测试基类，提供通用的测试工具方法
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly string TestTempDirectory;
        protected readonly string TestDataDirectory;

        protected TestBase()
        {
            // 首先重置ConfigManager状态，确保每个测试都从干净状态开始
            ResetConfigManager();
            
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

        public virtual void Dispose()
        {
            // 重置ConfigManager的配置路径和缓存
            ResetConfigManager();
            
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
        private void ResetConfigManager()
        {
            try
            {
                // 使用反射重置ConfigManager的私有静态字段
                var configManagerType = typeof(ConfigManager);
                
                // 重置自定义配置路径
                var customConfigPathField = configManagerType.GetField("_customConfigPath", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                customConfigPathField?.SetValue(null, null);
                
                // 重置缓存的配置对象
                var formatterConfigField = configManagerType.GetField("_formatterConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                formatterConfigField?.SetValue(null, null);
                
                var analyzerConfigField = configManagerType.GetField("_analyzerConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                analyzerConfigField?.SetValue(null, null);
                
                var usingRemoverConfigField = configManagerType.GetField("_usingRemoverConfig", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                usingRemoverConfigField?.SetValue(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重置ConfigManager失败: {ex.Message}");
            }
        }
    }
} 