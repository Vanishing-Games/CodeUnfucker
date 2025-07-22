using System;
using System.IO;
using System.Threading.Tasks;
using CodeUnfucker.Commands;
using CodeUnfucker.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodeUnfucker.Tests
{
    public class RemoveUsingCommandTests
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly Mock<IFileService> _fileServiceMock = new();
        private readonly RemoveUsingCommand _command;

        public RemoveUsingCommandTests()
        {
            _command = new RemoveUsingCommand(_loggerMock.Object, _fileServiceMock.Object);
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
        public async Task ExecuteAsync_ShouldReturnFalse_WhenPathInvalid()
        {
            _fileServiceMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            _fileServiceMock.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);
            var result = await _command.ExecuteAsync("invalid");
            result.Should().BeFalse();
        }
    }
}