using System;
using System.IO;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// ConfigManager 的单元测试
    /// </summary>
    public class ConfigManagerTests : TestBase
    {
        [Fact]
        public void GetFormatterConfig_ShouldReturnDefaultConfig_WhenNoConfigFileExists()
        {
            // Arrange - 首先重置ConfigManager状态，然后使用不存在的配置目录
            ResetConfigManager();
            ConfigManager.SetConfigPath(Path.Combine(TestTempDirectory, "NonExistent"));

            // Act
            var config = ConfigManager.GetFormatterConfig();

            // Assert
            config.Should().NotBeNull();
            config.FormatterSettings.MinLinesForRegion.Should().Be(15);
            config.FormatterSettings.EnableRegionGeneration.Should().BeTrue();
            config.FormatterSettings.CreateBackupFiles.Should().BeTrue();
            config.FormatterSettings.BackupFileExtension.Should().Be(".backup");
            config.FormatterSettings.FormatterType.Should().Be(FormatterType.Built_in);
        }

        [Fact]
        public void GetFormatterConfig_ShouldLoadCustomConfig_WhenValidConfigFileExists()
        {
            // 不使用ExecuteWithConfigIsolation，直接管理配置状态
            // Arrange - 首先重置ConfigManager状态
            ResetConfigManager();
            
            var customConfig = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    MinLinesForRegion = 10
                }
            };

            // 使用与成功测试相同的方法创建配置文件，但使用独立的目录
            var configDir = Path.Combine(TestTempDirectory, "CustomConfig");
            Directory.CreateDirectory(configDir);
            var configFile = Path.Combine(configDir, "FormatterConfig.json");
            var jsonContent = JsonSerializer.Serialize(customConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configFile, jsonContent);
            
            ConfigManager.SetConfigPath(configDir);
            
            // 强制重新加载配置，确保不会使用缓存
            ConfigManager.ReloadConfigs();

            // 验证配置文件是否被正确创建
            Console.WriteLine($"配置文件路径: {configFile}");
            Console.WriteLine($"配置文件是否存在: {File.Exists(configFile)}");
            if (File.Exists(configFile))
            {
                Console.WriteLine($"配置文件内容: {File.ReadAllText(configFile)}");
            }

            // 验证ConfigManager使用的配置路径
            var configManagerType = typeof(ConfigManager);
            var customConfigPathField = configManagerType.GetField("_customConfigPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var actualCustomPath = customConfigPathField?.GetValue(null);
            Console.WriteLine($"ConfigManager._customConfigPath: {actualCustomPath}");

            // 验证ConfigManager实际尝试加载的配置文件路径
            var expectedConfigFile = Path.Combine(configDir, "FormatterConfig.json");
            Console.WriteLine($"ConfigManager应该加载的配置文件: {expectedConfigFile}");
            Console.WriteLine($"该文件是否存在: {File.Exists(expectedConfigFile)}");

            // 验证ConfigPath属性返回的值
            var configPathProperty = configManagerType.GetProperty("ConfigPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var actualConfigPath = configPathProperty?.GetValue(null);
            Console.WriteLine($"ConfigManager.ConfigPath: {actualConfigPath}");

            // Act
            var config = ConfigManager.GetFormatterConfig();
            Console.WriteLine($"实际FormatterSettings: {System.Text.Json.JsonSerializer.Serialize(config.FormatterSettings)}");
            
            // 验证实际加载的配置文件内容
            var actualConfigFile = Path.Combine(actualConfigPath?.ToString() ?? "", "FormatterConfig.json");
            Console.WriteLine($"实际加载的配置文件路径: {actualConfigFile}");
            if (File.Exists(actualConfigFile))
            {
                Console.WriteLine($"实际加载的配置文件内容: {File.ReadAllText(actualConfigFile)}");
            }

            // Assert
            config.FormatterSettings.MinLinesForRegion.Should().Be(10);
        }

        [Fact]
        public void SetConfigPath_ShouldUpdateConfigPath_AndReloadConfigs()
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange - 首先重置ConfigManager状态
                ResetConfigManager();
                
                var configPath = Path.Combine(TestTempDirectory, "CustomConfig");
                Directory.CreateDirectory(configPath);
                
                var customConfig = new FormatterConfig
                {
                    FormatterSettings = new FormatterSettings
                    {
                        MinLinesForRegion = 99
                    }
                };

                var configFile = Path.Combine(configPath, "FormatterConfig.json");
                var jsonContent = JsonSerializer.Serialize(customConfig, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configFile, jsonContent);

                // Act
                ConfigManager.SetConfigPath(configPath);
                var config = ConfigManager.GetFormatterConfig();

                // Assert
                config.FormatterSettings.MinLinesForRegion.Should().Be(99);
            });
        }

        [Fact]
        public void GetFormatterConfig_ShouldReturnDefaultConfig_WhenConfigFileIsInvalid()
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange - 首先重置ConfigManager状态，然后创建无效的JSON文件
                ResetConfigManager();
                
                var configDir = Path.Combine(TestTempDirectory, "Config");
                Directory.CreateDirectory(configDir);
                var configFile = Path.Combine(configDir, "FormatterConfig.json");
                File.WriteAllText(configFile, "{ invalid json }");

                ConfigManager.SetConfigPath(configDir);

                // Act
                var config = ConfigManager.GetFormatterConfig();

                // Assert
                config.Should().NotBeNull();
                config.FormatterSettings.MinLinesForRegion.Should().Be(15); // 默认值
            });
        }

        [Fact]
        public void ReloadConfigs_ShouldClearCachedConfigs()
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange
                var configDir = Path.Combine(TestTempDirectory, "Config");
                Directory.CreateDirectory(configDir);

                // 创建初始配置
                var initialConfig = new FormatterConfig
                {
                    FormatterSettings = new FormatterSettings { MinLinesForRegion = 5 }
                };
                CreateTempConfigFile("FormatterConfig.json", initialConfig);
                ConfigManager.SetConfigPath(configDir);

                // 加载初始配置
                var firstLoad = ConfigManager.GetFormatterConfig();
                firstLoad.FormatterSettings.MinLinesForRegion.Should().Be(5);

                // 修改配置文件，并确保文件系统操作完成
                var updatedConfig = new FormatterConfig
                {
                    FormatterSettings = new FormatterSettings { MinLinesForRegion = 20 }
                };
                CreateTempConfigFile("FormatterConfig.json", updatedConfig);
                
                // 强制等待，确保文件系统操作完成
                System.Threading.Thread.Sleep(50);

                // Act - 多次调用确保重载
                ConfigManager.ReloadConfigs();
                ConfigManager.ReloadConfigs(); // 双重重载确保缓存清理
                var secondLoad = ConfigManager.GetFormatterConfig();

                // Assert
                secondLoad.FormatterSettings.MinLinesForRegion.Should().Be(20);
            });
        }

        [Fact]
        public void GetFormatterConfig_ShouldLoadUnityLifeCycleMethods_FromConfig()
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange - 首先重置ConfigManager状态
                ResetConfigManager();
                
                var customConfig = new FormatterConfig
                {
                    FormatterSettings = new FormatterSettings(),
                    UnityLifeCycleMethods = new() { "CustomAwake", "CustomStart", "CustomUpdate" }
                };

                CreateTempConfigFile("FormatterConfig.json", customConfig);
                var configDir = Path.Combine(TestTempDirectory, "Config");
                ConfigManager.SetConfigPath(configDir);
                
                // 强制重新加载配置，确保不会使用缓存
                ConfigManager.ReloadConfigs();

                // Act
                var config = ConfigManager.GetFormatterConfig();

                // Assert
                config.UnityLifeCycleMethods.Should().Contain("CustomAwake");
                config.UnityLifeCycleMethods.Should().Contain("CustomStart");
                config.UnityLifeCycleMethods.Should().Contain("CustomUpdate");
                config.UnityLifeCycleMethods.Should().HaveCount(3);
            });
        }

        [Theory]
        [InlineData("Built_in", FormatterType.Built_in)]
        [InlineData("CSharpier", FormatterType.CSharpier)]
        public void GetFormatterConfig_ShouldParseFormatterType_Correctly(string enumString, FormatterType expectedType)
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange - 首先重置ConfigManager状态
                ResetConfigManager();
                
                var configDir = Path.Combine(TestTempDirectory, "Config");
                Directory.CreateDirectory(configDir);
                var configFile = Path.Combine(configDir, "FormatterConfig.json");
                
                var jsonContent = $$"""
                {
                    "formatterSettings": {
                        "formatterType": "{{enumString}}"
                    }
                }
                """;
                File.WriteAllText(configFile, jsonContent);

                ConfigManager.SetConfigPath(configDir);

                // Act
                var config = ConfigManager.GetFormatterConfig();

                // Assert
                config.FormatterSettings.FormatterType.Should().Be(expectedType);
            });
        }
    }
} 