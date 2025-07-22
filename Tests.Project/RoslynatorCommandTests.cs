using System;
using System.Threading.Tasks;
using CodeUnfucker.Commands;
using CodeUnfucker.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodeUnfucker.Tests
{
    public class RoslynatorCommandTests
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly Mock<IFileService> _fileServiceMock = new();
        private readonly RoslynatorCommand _command;

        public RoslynatorCommandTests()
        {
            _command = new RoslynatorCommand(_loggerMock.Object, _fileServiceMock.Object);
        }

        [Fact]
        public void ValidateParameters_ShouldReturnFalse_WhenPathNotExist()
        {
            _fileServiceMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            _fileServiceMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
            var result = _command.ValidateParameters("notfound.cs");
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateParameters_ShouldReturnTrue_WhenFileExists()
        {
            _fileServiceMock.Setup(f => f.FileExists("file.cs")).Returns(true);
            var result = _command.ValidateParameters("file.cs");
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateParameters_ShouldReturnTrue_WhenDirectoryExists()
        {
            _fileServiceMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            _fileServiceMock.Setup(f => f.DirectoryExists("dir")).Returns(true);
            var result = _command.ValidateParameters("dir");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnTrue_WhenNoException()
        {
            // 只验证主流程不抛异常即可
            var result = await _command.ExecuteAsync("file.cs");
            result.Should().BeTrue();
        }
    }
}