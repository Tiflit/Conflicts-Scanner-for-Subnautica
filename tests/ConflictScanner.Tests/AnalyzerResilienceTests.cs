using System;
using System.IO;
using ConflictScanner;
using ConflictScanner.Analysis;
using Xunit;

namespace ConflictScanner.Tests
{
    public class AnalyzerResilienceTests : IDisposable
    {
        private readonly string _tempDir;

        public AnalyzerResilienceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_Resilience_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [Fact]
        public void HarmonyAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new HarmonyAnalyzer();
            analyzer.Run(context);
            Assert.Empty(context.Findings);
        }

        [Fact]
        public void NautilusAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new NautilusAnalyzer();
            analyzer.Run(context);
            Assert.Empty(context.Findings);
        }

        [Fact]
        public void BepInPluginAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new BepInPluginAnalyzer();
            analyzer.Run(context);
            Assert.Empty(context.Findings);
        }

        [Fact]
        public void PatcherAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new PatcherAnalyzer();
            analyzer.Run(context);
        }

        [Fact]
        public void QModAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new QModAnalyzer();
            analyzer.Run(context);
        }

        [Fact]
        public void SMLHelperAnalyzer_NonExistentPath_DoesNotThrow()
        {
            var context = new ScanContext(Path.Combine(_tempDir, "empty"), ScanMode.Quick, "Subnautica");
            var analyzer = new SMLHelperAnalyzer();
            analyzer.Run(context);
        }
    }
}
