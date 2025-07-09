using Xunit;

namespace CodeUnfucker.Tests
{
    public class SimpleTest
    {
        [Fact]
        public void SimpleTest_ShouldPass()
        {
            // Arrange
            var expected = 1;
            
            // Act
            var actual = 1;
            
            // Assert
            Assert.Equal(expected, actual);
        }
    }
} 