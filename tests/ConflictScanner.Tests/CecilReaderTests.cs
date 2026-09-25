using System.IO;
using ConflictScanner.Analysis;
using Xunit;

namespace ConflictScanner.Tests
{
    public class CecilReaderTests
    {
        [Fact]
        public void AnalyzeAssembly_NonExistentFile_ReturnsNull()
        {
            var result = CecilAssemblyReader.AnalyzeAssembly("non_existent_file.dll", "TestMod");
            Assert.Null(result);
        }

        [Fact]
        public void AnalyzeAssembly_ValidDotNetAssembly_ReadsSuccessfully()
        {
            // Inspect ConflictScanner's own assembly using Cecil
            string asmPath = typeof(CecilAssemblyReader).Assembly.Location;
            if (File.Exists(asmPath))
            {
                var result = CecilAssemblyReader.AnalyzeAssembly(asmPath, "ConflictScanner");
                Assert.NotNull(result);
                Assert.Equal("ConflictScanner", result.ModName);
            }
        }
    }
}
