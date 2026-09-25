using ConflictScanner;
using Xunit;

namespace ConflictScanner.Tests
{
    public class IgnoreListTests
    {
        [Theory]
        [InlineData("README.md", true)]
        [InlineData("readme.txt", true)]
        [InlineData("changelog.txt", true)]
        [InlineData("LICENSE", true)]
        [InlineData(".gitignore", true)]
        [InlineData(".gitkeep", true)]
        [InlineData("icon.png", false)]
        [InlineData("config.json", false)]
        [InlineData("bundle", false)]
        [InlineData("assets/sound.ogg", false)]
        public void ShouldIgnore_DetectsExpectedFiles(string path, bool expected)
        {
            bool ignored = IgnoreList.ShouldIgnore(path);
            Assert.Equal(expected, ignored);
        }
    }
}
