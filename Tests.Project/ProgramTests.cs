using System;
using System.IO;
using Xunit;
using FluentAssertions;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// Program 类的单元测试
    /// </summary>
    public class ProgramTests : TestBase
    {
        [Fact]
        public void ValidateArgs_ShouldParseAnalyzeCommand_Correctly()
        {
            // Arrange
            var args = new[] { "analyze", TestTempDirectory };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out string? configPath);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("analyze");
            path.Should().Be(TestTempDirectory);
            configPath.Should().BeNull();
        }

        [Fact]
        public void ValidateArgs_ShouldParseFormatCommand_Correctly()
        {
            // Arrange
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "format", testFile };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out string? configPath);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("format");
            path.Should().Be(testFile);
            configPath.Should().BeNull();
        }

        [Fact]
        public void ValidateArgs_ShouldParseCSharpierCommand_Correctly()
        {
            // Arrange
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "csharpier", testFile };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out string? configPath);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("csharpier");
            path.Should().Be(testFile);
            configPath.Should().BeNull();
        }

        [Fact]
        public void ValidateArgs_ShouldParseCommandWithConfig_Correctly()
        {
            // Arrange
            var args = new[] { "analyze", TestTempDirectory, "--config", TestTempDirectory };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out string? configPath);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("analyze");
            path.Should().Be(TestTempDirectory);
            configPath.Should().Be(TestTempDirectory);
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForHelpArguments()
        {
            // Arrange
            var helpArgs = new[] { "--help" };

            // Act
            var result = Program.ValidateArgs(helpArgs, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForShortHelp()
        {
            // Arrange
            var args = new[] { "-h" };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForHelpCommand()
        {
            // Arrange
            var args = new[] { "help" };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldSupportBackwardsCompatibility_WithSingleArgument()
        {
            // Arrange
            var args = new[] { TestTempDirectory };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out string? configPath);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("analyze");
            path.Should().Be(TestTempDirectory);
            configPath.Should().BeNull();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_WhenAnalyzePathDoesNotExist()
        {
            // Arrange
            var nonExistentPath = Path.Combine(TestTempDirectory, "nonexistent");
            var args = new[] { "analyze", nonExistentPath };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_WhenFormatPathDoesNotExist()
        {
            // Arrange
            var nonExistentPath = Path.Combine(TestTempDirectory, "nonexistent.cs");
            var args = new[] { "format", nonExistentPath };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnTrue_WhenFormatPathIsValidFile()
        {
            // Arrange
            var validFile = CreateTempFile("test.cs", "// test content");
            var args = new[] { "format", validFile };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out _);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("format");
            path.Should().Be(validFile);
        }

        [Fact]
        public void ValidateArgs_ShouldReturnTrue_WhenFormatPathIsValidDirectory()
        {
            // Arrange
            var args = new[] { "format", TestTempDirectory };

            // Act
            var result = Program.ValidateArgs(args, out string command, out string path, out _);

            // Assert
            result.Should().BeTrue();
            command.Should().Be("format");
            path.Should().Be(TestTempDirectory);
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForEmptyArgs()
        {
            // Arrange
            var args = new string[] { };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForMissingPath()
        {
            // Arrange
            var args = new[] { "analyze" };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_ForTooManyArgs()
        {
            // Arrange
            var args = new[] { "analyze", "path", "extra" };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateArgs_ShouldReturnFalse_WhenConfigFlagMissingValue()
        {
            // Arrange
            var validFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "format", validFile, "--config" };

            // Act
            var result = Program.ValidateArgs(args, out _, out _, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Run_ShouldHandleAnalyzeCommand()
        {
            // Arrange
            CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var program = new Program();
            var args = new[] { "analyze", TestTempDirectory };

            // Act & Assert
            // 这个测试主要验证不会抛出异常
            Action runAction = () => program.Run(args);
            runAction.Should().NotThrow();
        }

        [Fact]
        public void Run_ShouldHandleFormatCommand()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var program = new Program();
            var args = new[] { "format", testFile };

            // Act & Assert
            Action runAction = () => program.Run(args);
            runAction.Should().NotThrow();
        }

        [Fact]
        public void Run_ShouldHandleCSharpierCommand()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var program = new Program();
            var args = new[] { "csharpier", testFile };

            // Act & Assert
            Action runAction = () => program.Run(args);
            runAction.Should().NotThrow();
        }

        [Fact]
        public void Run_ShouldHandleUnknownCommand()
        {
            // Arrange
            var program = new Program();
            var args = new[] { "unknown", TestTempDirectory };

            // Act & Assert
            Action runAction = () => program.Run(args);
            runAction.Should().NotThrow();
        }

        [Fact]
        public void Run_ShouldSetupConfig_WhenConfigPathProvided()
        {
            ExecuteWithConfigIsolation(() =>
            {
                // Arrange
                var configDir = Path.Combine(TestTempDirectory, "CustomConfig");
                Directory.CreateDirectory(configDir);
                
                var config = new FormatterConfig
                {
                    FormatterSettings = new FormatterSettings
                    {
                        MinLinesForRegion = 99
                    }
                };
                
                var configFile = Path.Combine(configDir, "FormatterConfig.json");
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configFile, jsonContent);

                var testFile = CreateTempFile("TestFile.cs", "public class Test { }");
                var program = new Program();
                var args = new[] { "format", testFile, "--config", configDir };

                // Act & Assert
                Action runAction = () => program.Run(args);
                runAction.Should().NotThrow();
                
                // 验证配置已加载
                var loadedConfig = ConfigManager.GetFormatterConfig();
                loadedConfig.FormatterSettings.MinLinesForRegion.Should().Be(99);
            });
        }

        [Fact]
        public void Run_ShouldHandleInvalidArguments()
        {
            // Arrange
            var program = new Program();
            var invalidArgs = new[] { "invalid" };

            // Act & Assert
            Action runAction = () => program.Run(invalidArgs);
            runAction.Should().NotThrow();
        }

        [Fact]
        public void ValidateArgs_ShouldBeCaseInsensitive_ForAnalyze()
        {
            // Arrange
            var args = new[] { "ANALYZE", TestTempDirectory };

            // Act
            var result = Program.ValidateArgs(args, out string parsedCommand, out _, out _);

            // Assert
            result.Should().BeTrue();
            parsedCommand.Should().Be("ANALYZE");
        }

        [Fact]
        public void ValidateArgs_ShouldBeCaseInsensitive_ForFormat()
        {
            // Arrange
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "Format", testFile };

            // Act
            var result = Program.ValidateArgs(args, out string parsedCommand, out _, out _);

            // Assert
            result.Should().BeTrue();
            parsedCommand.Should().Be("Format");
        }

        [Fact]
        public void ValidateArgs_ShouldBeCaseInsensitive_ForCSharpier()
        {
            // Arrange
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "CSHARPIER", testFile };

            // Act
            var result = Program.ValidateArgs(args, out string parsedCommand, out _, out _);

            // Assert
            result.Should().BeTrue();
            parsedCommand.Should().Be("CSHARPIER");
        }

        [Fact]
        public void Run_ShouldHandleNonExistentConfigPath()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", "public class Test { }");
            var program = new Program();
            var nonExistentConfigPath = Path.Combine(TestTempDirectory, "NonExistent");
            var args = new[] { "format", testFile, "--config", nonExistentConfigPath };

            // Act & Assert
            Action runAction = () => program.Run(args);
            runAction.Should().NotThrow();
        }
    }
} 