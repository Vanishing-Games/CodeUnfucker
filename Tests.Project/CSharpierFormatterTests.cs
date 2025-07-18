using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// CSharpierFormatter 的单元测试
    /// </summary>
    public class CSharpierFormatterTests : TestBase
    {
        private const string SampleCode = @"
using System;

public class TestClass
{
    public void Method()
    {
        Console.WriteLine(""Hello World"");
    }
}";

        [Fact]
        public void FormatCode_ShouldReturnOriginalCode_WhenCSharpierNotImplemented()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act
            var result = formatter.FormatCode(SampleCode, "TestClass.cs");

            // Assert
            result.Should().Be(SampleCode);
        }

        [Fact]
        public async Task FormatCodeAsync_ShouldReturnOriginalCode_WhenCSharpierNotImplemented()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act
            var result = await formatter.FormatCodeAsync(SampleCode, "TestClass.cs");

            // Assert
            result.Should().Be(SampleCode);
        }

        [Fact]
        public void FormatCode_ShouldHandleEmptyString()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act
            var result = formatter.FormatCode(string.Empty, "TestClass.cs");

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void FormatCode_ShouldHandleNullInput()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act & Assert
            // 由于当前实现直接返回输入，null会导致异常
            // 这个测试验证当前的行为
            var result = formatter.FormatCode(null!, "TestClass.cs");
            result.Should().BeNull();
        }

        [Fact]
        public void FormatCode_ShouldHandleInvalidCode()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();
            var invalidCode = "{ invalid C# code }";

            // Act
            var result = formatter.FormatCode(invalidCode, "TestClass.cs");

            // Assert
            // 当前实现返回原始代码，即使是无效的
            result.Should().Be(invalidCode);
        }

        [Fact]
        public void FormatCode_ShouldHandleLargeFile()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();
            
            // 创建一个大文件内容
            var largeCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

public class LargeClass
{
";
            for (int i = 0; i < 100; i++)
            {
                largeCode += $@"
    public void Method{i}()
    {{
        Console.WriteLine(""Method {i}"");
        var list = new List<int> {{ {string.Join(", ", Enumerable.Range(1, 10))} }};
        foreach (var item in list)
        {{
            Console.WriteLine($""Item: {{item}}"");
        }}
    }}
";
            }
            largeCode += "}";

            // Act
            var result = formatter.FormatCode(largeCode, "LargeClass.cs");

            // Assert
            result.Should().Be(largeCode);
        }

        [Theory]
        [InlineData("TestClass.cs")]
        [InlineData("MyClass.cs")]
        [InlineData("../folder/AnotherClass.cs")]
        public void FormatCode_ShouldAcceptDifferentFilePaths(string filePath)
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act
            var result = formatter.FormatCode(SampleCode, filePath);

            // Assert
            result.Should().Be(SampleCode);
        }

        [Fact]
        public async Task FormatCodeAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CSharpierFormatter();

            // Act
            var task = formatter.FormatCodeAsync(SampleCode, "TestClass.cs");
            
            // Assert
            task.Should().NotBeNull();
            var result = await task;
            result.Should().Be(SampleCode);
        }

        [Fact]
        public void Constructor_ShouldNotThrow()
        {
            // Arrange & Act
            Action createFormatter = () => new CSharpierFormatter();

            // Assert
            createFormatter.Should().NotThrow();
        }

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
        public void FormatCode_ShouldUseConfigFromConfigManager()
        {
            // Arrange
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    FormatterType = FormatterType.CSharpier
                }
            };
            var configDir = SetupUniqueTestConfig(config);
            var formatter = new CSharpierFormatter();
            // Act & Assert
            Action formatAction = () => formatter.FormatCode(SampleCode, "TestClass.cs");
            formatAction.Should().NotThrow();
            // 清理
            Directory.Delete(configDir, true);
        }

        private void SetupTestConfig()
        {
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    FormatterType = FormatterType.CSharpier
                }
            };
            var configDir = SetupUniqueTestConfig(config);
            // 不再用TestTempDirectory
        }
    }
} 