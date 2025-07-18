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
            // --- 强制清理ConfigManager静态缓存和路径 ---
            var configManagerType = typeof(ConfigManager);
            var formatterConfigField = configManagerType.GetField("_formatterConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var customConfigPathField = configManagerType.GetField("_customConfigPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            formatterConfigField?.SetValue(null, null);
            customConfigPathField?.SetValue(null, null);

            // Arrange
            var configDir = Path.Combine(Path.GetTempPath(), "CodeUnfuckerTest_" + Guid.NewGuid());
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

            ConfigManager.SetConfigPath(configDir);
            ConfigManager.ReloadConfigs();

            // Act
            var config = ConfigManager.GetFormatterConfig();

            // Assert
            config.FormatterSettings.MinLinesForRegion.Should().Be(10);

            // 清理
            Directory.Delete(configDir, true);
            // --- 再次清理，防止影响其他测试 ---
            formatterConfigField?.SetValue(null, null);
            customConfigPathField?.SetValue(null, null);
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