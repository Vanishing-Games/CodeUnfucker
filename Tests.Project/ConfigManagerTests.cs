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
        private string SetupUniqueTestConfig(FormatterConfig config)
        {
            var configDir = Path.Combine(Path.GetTempPath(), "CodeUnfuckerTest_Unique_" + Guid.NewGuid());
            Directory.CreateDirectory(configDir);
            var configFile = Path.Combine(configDir, "FormatterConfig.json");
            var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configFile, jsonContent);
            // 清理ConfigManager缓存
            var configManagerType = typeof(ConfigManager);
            var formatterConfigField = configManagerType.GetField("_formatterConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var customConfigPathField = configManagerType.GetField("_customConfigPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            formatterConfigField?.SetValue(null, null);
            customConfigPathField?.SetValue(null, null);
            ConfigManager.SetConfigPath(configDir);
            ConfigManager.ReloadConfigs();
            return configDir;
        }

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
            // Arrange
            var configDir = Path.Combine(Path.GetTempPath(), "CodeUnfuckerTest_Unique_" + Guid.NewGuid());
            Directory.CreateDirectory(configDir);

            var customConfig = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    MinLinesForRegion = 10
                }
            };
            var configFile = Path.Combine(configDir, "FormatterConfig.json");
            var jsonContent = JsonSerializer.Serialize(customConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configFile, jsonContent);
            // 立即输出写入内容
            Console.WriteLine($"[TEST] 写入配置文件: {configFile}");
            Console.WriteLine($"[TEST] 写入内容: {File.ReadAllText(configFile)}");

            // 彻底清理ConfigManager缓存
            var configManagerType = typeof(ConfigManager);
            var formatterConfigField = configManagerType.GetField("_formatterConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var customConfigPathField = configManagerType.GetField("_customConfigPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            formatterConfigField?.SetValue(null, null);
            customConfigPathField?.SetValue(null, null);

            ConfigManager.SetConfigPath(configDir);
            ConfigManager.ReloadConfigs();

            // 再次输出ConfigManager.ConfigPath和实际加载的文件内容
            var configPathProperty = configManagerType.GetProperty("ConfigPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var actualConfigPath = configPathProperty?.GetValue(null)?.ToString();
            var actualConfigFile = Path.Combine(actualConfigPath ?? "", "FormatterConfig.json");
            Console.WriteLine($"[TEST] ConfigManager.ConfigPath: {actualConfigPath}");
            if (File.Exists(actualConfigFile))
                Console.WriteLine($"[TEST] 实际加载的配置内容: {File.ReadAllText(actualConfigFile)}");
            else
                Console.WriteLine($"[TEST] 实际加载的配置文件不存在: {actualConfigFile}");

            // Act
            var config = ConfigManager.GetFormatterConfig();

            // Assert
            Console.WriteLine($"[TEST] 实际MinLinesForRegion: {config.FormatterSettings.MinLinesForRegion}");
            config.FormatterSettings.MinLinesForRegion.Should().Be(10);

            // 清理
            Directory.Delete(configDir, true);
        }

        [Fact]
        public void SetConfigPath_ShouldUpdateConfigPath_AndReloadConfigs()
        {
            // Arrange
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    MinLinesForRegion = 99
                }
            };
            var configDir = SetupUniqueTestConfig(config);
            // Act
            ConfigManager.SetConfigPath(configDir);
            var loadedConfig = ConfigManager.GetFormatterConfig();
            // Assert
            loadedConfig.FormatterSettings.MinLinesForRegion.Should().Be(99);
            // 清理
            Directory.Delete(configDir, true);
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
            // Arrange
            var initialConfig = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings { MinLinesForRegion = 5 }
            };
            var configDir = SetupUniqueTestConfig(initialConfig);
            ConfigManager.SetConfigPath(configDir);
            var firstLoad = ConfigManager.GetFormatterConfig();
            firstLoad.FormatterSettings.MinLinesForRegion.Should().Be(5);
            // 修改配置文件
            var updatedConfig = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings { MinLinesForRegion = 20 }
            };
            var configFile = Path.Combine(configDir, "FormatterConfig.json");
            var jsonContent = JsonSerializer.Serialize(updatedConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configFile, jsonContent);
            System.Threading.Thread.Sleep(50);
            // Act
            ConfigManager.ReloadConfigs();
            var secondLoad = ConfigManager.GetFormatterConfig();
            // Assert
            secondLoad.FormatterSettings.MinLinesForRegion.Should().Be(20);
            // 清理
            Directory.Delete(configDir, true);
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