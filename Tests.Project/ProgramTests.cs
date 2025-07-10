using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using CodeUnfucker.Services;
using CodeUnfucker.Commands;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// Program 类的单元测试 - 重构后版本
    /// </summary>
    public class ProgramTests : TestBase
    {
        // 移除Dispose重写，因为现在不使用单例ServiceContainer了
        
        /// <summary>
        /// 创建独立的ApplicationService实例，避免单例状态污染
        /// </summary>
        private ApplicationService CreateApplicationService()
        {
            var logger = new ConsoleLogger();
            var fileService = new FileService(logger);
            var commandLineParser = new CommandLineParser(logger);
            var commandRegistry = new CommandRegistry(logger, fileService);
            return new ApplicationService(logger, fileService, commandLineParser, commandRegistry);
        }
        [Fact]
        public void Parse_ShouldParseAnalyzeCommand_Correctly()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "analyze", TestTempDirectory };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("analyze");
            result.Path.Should().Be(TestTempDirectory);
            result.ConfigPath.Should().BeNull();
        }

        [Fact]
        public void Parse_ShouldParseFormatCommand_Correctly()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "format", testFile };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("format");
            result.Path.Should().Be(testFile);
            result.ConfigPath.Should().BeNull();
        }

        [Fact]
        public void Parse_ShouldParseCSharpierCommand_Correctly()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "csharpier", testFile };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("csharpier");
            result.Path.Should().Be(testFile);
            result.ConfigPath.Should().BeNull();
        }

        [Fact]
        public void Parse_ShouldParseCommandWithConfig_Correctly()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "analyze", TestTempDirectory, "--config", TestTempDirectory };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("analyze");
            result.Path.Should().Be(TestTempDirectory);
            result.ConfigPath.Should().Be(TestTempDirectory);
        }

        [Fact]
        public void Parse_ShouldReturnShowHelp_ForHelpArguments()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var helpArgs = new[] { "--help" };

            // Act
            var result = parser.Parse(helpArgs);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void Parse_ShouldReturnShowHelp_ForShortHelp()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "-h" };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void Parse_ShouldReturnShowHelp_ForHelpCommand()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "help" };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void Parse_ShouldSupportBackwardsCompatibility_WithSingleArgument()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { TestTempDirectory };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("analyze");
            result.Path.Should().Be(TestTempDirectory);
            result.ConfigPath.Should().BeNull();
        }

        [Fact]
        public void Parse_ShouldReturnInvalid_ForEmptyArgs()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new string[] { };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Parse_ShouldReturnInvalid_ForMissingPath()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "analyze" };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Parse_ShouldReturnInvalid_ForTooManyArgs()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "analyze", "path", "extra" };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Parse_ShouldReturnInvalid_WhenConfigFlagMissingValue()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var validFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "format", validFile, "--config" };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RunAsync_ShouldHandleAnalyzeCommand()
        {
            // Arrange
            CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var applicationService = CreateApplicationService();
            var args = new[] { "analyze", TestTempDirectory };

            // Act & Assert
            Func<Task> runAction = async () => await applicationService.RunAsync(args);
            await runAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RunAsync_ShouldHandleFormatCommand()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var applicationService = CreateApplicationService();
            var args = new[] { "format", testFile };

            // Act & Assert
            Func<Task> runAction = async () => await applicationService.RunAsync(args);
            await runAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RunAsync_ShouldHandleCSharpierCommand()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", @"
public class TestClass
{
    public void Method() { }
}");

            var applicationService = CreateApplicationService();
            var args = new[] { "csharpier", testFile };

            // Act & Assert
            Func<Task> runAction = async () => await applicationService.RunAsync(args);
            await runAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RunAsync_ShouldHandleUnknownCommand()
        {
            // Arrange
            var applicationService = CreateApplicationService();
            var args = new[] { "unknown", TestTempDirectory };

            // Act
            var result = await applicationService.RunAsync(args);

            // Assert
            result.Should().BeFalse(); // 未知命令应该返回 false
        }

        [Fact]
        public async Task RunAsync_ShouldSetupConfig_WhenConfigPathProvided()
        {
            // Arrange
            var configDir = Path.Combine(TestTempDirectory, "CustomConfig");
            Directory.CreateDirectory(configDir);
            
            var testFile = CreateTempFile("TestFile.cs", "public class Test { }");
            var applicationService = CreateApplicationService();
            var args = new[] { "format", testFile, "--config", configDir };

            // Act
            var result = await applicationService.RunAsync(args);

            // Assert - 主要验证不会崩溃，配置功能正常
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RunAsync_ShouldHandleInvalidArguments()
        {
            // Arrange
            var applicationService = CreateApplicationService();
            var invalidArgs = new[] { "invalid" };

            // Act
            var result = await applicationService.RunAsync(invalidArgs);

            // Assert
            result.Should().BeFalse(); // 无效参数应该返回 false
        }

        [Fact]
        public void Parse_ShouldBeCaseInsensitive_ForAnalyze()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var args = new[] { "ANALYZE", TestTempDirectory };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("ANALYZE");
        }

        [Fact]
        public void Parse_ShouldBeCaseInsensitive_ForFormat()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "Format", testFile };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("Format");
        }

        [Fact]
        public void Parse_ShouldBeCaseInsensitive_ForCSharpier()
        {
            // Arrange
            var logger = new ConsoleLogger();
            var parser = new CommandLineParser(logger);
            var testFile = CreateTempFile("test.cs", "// test");
            var args = new[] { "CSHARPIER", testFile };

            // Act
            var result = parser.Parse(args);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Command.Should().Be("CSHARPIER");
        }

        [Fact]
        public async Task RunAsync_ShouldHandleNonExistentConfigPath()
        {
            // Arrange
            var testFile = CreateTempFile("TestFile.cs", "public class Test { }");
            var applicationService = CreateApplicationService();
            var nonExistentConfigPath = Path.Combine(TestTempDirectory, "NonExistent");
            var args = new[] { "format", testFile, "--config", nonExistentConfigPath };

            // Act & Assert
            Func<Task> runAction = async () => await applicationService.RunAsync(args);
            await runAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RunAsync_ShouldReturnTrue_ForHelpCommand()
        {
            // Arrange
            var applicationService = CreateApplicationService();
            var args = new[] { "--help" };

            // Act
            var result = await applicationService.RunAsync(args);

            // Assert
            result.Should().BeTrue(); // 帮助命令应该成功
        }

        [Fact]
        public async Task RunAsync_ShouldValidateCommandParameters()
        {
            // Arrange
            var applicationService = CreateApplicationService();
            var nonExistentPath = Path.Combine(TestTempDirectory, "nonexistent_directory_that_should_not_exist");
            
            // 确保路径确实不存在
            if (Directory.Exists(nonExistentPath))
            {
                Directory.Delete(nonExistentPath, true);
            }
            
            var args = new[] { "analyze", nonExistentPath };

            // Act
            var result = await applicationService.RunAsync(args);

            // Assert
            result.Should().BeFalse(); // 无效路径应该返回 false
        }
    }
} 