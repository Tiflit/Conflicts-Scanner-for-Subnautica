using System;
using System.IO;
using System.Linq;
using ConflictScanner;
using Xunit;

namespace ConflictScanner.Tests
{
    public class FileOverrideAnalyzerTests : IDisposable
    {
        private readonly string _gameDir;

        public FileOverrideAnalyzerTests()
        {
            _gameDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_Game_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_gameDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_gameDir))
            {
                try { Directory.Delete(_gameDir, true); } catch { }
            }
        }

        [Fact]
        public void Run_IsolatedModAssets_DoNotReportPathConflicts()
        {
            // Setup BepInEx/plugins/ModA and BepInEx/plugins/ModB
            string modADir = Path.Combine(_gameDir, "BepInEx", "plugins", "ModA");
            string modBDir = Path.Combine(_gameDir, "BepInEx", "plugins", "ModB");
            Directory.CreateDirectory(modADir);
            Directory.CreateDirectory(modBDir);

            // Both mods have an icon.png and a config.json in their own isolated folders
            File.WriteAllBytes(Path.Combine(modADir, "icon.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            File.WriteAllBytes(Path.Combine(modBDir, "icon.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            File.WriteAllText(Path.Combine(modADir, "config.json"), "{\"optionA\": 1}");
            File.WriteAllText(Path.Combine(modBDir, "config.json"), "{\"optionB\": 2}");

            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");
            var analyzer = new FileOverrideAnalyzer();

            analyzer.Run(context);

            // Ensure no path conflict warnings are generated for isolated files
            Assert.DoesNotContain(context.FileWarnings, w => w.Message.Contains("Path conflict"));
        }

        [Fact]
        public void Run_DuplicateAssemblyNames_ReportsCollisionFinding()
        {
            string modADir = Path.Combine(_gameDir, "BepInEx", "plugins", "ModA");
            string modBDir = Path.Combine(_gameDir, "BepInEx", "plugins", "ModB");
            Directory.CreateDirectory(modADir);
            Directory.CreateDirectory(modBDir);

            // Both mods bundle the same third-party library DLL name
            File.WriteAllBytes(Path.Combine(modADir, "SharedLib.dll"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(modBDir, "SharedLib.dll"), new byte[] { 4, 5, 6 });

            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");
            var analyzer = new FileOverrideAnalyzer();

            analyzer.Run(context);

            Assert.Contains(context.FileWarnings, w => w.Message.Contains("SharedLib.dll") && w.Level == Severity.Warning);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Filesystem" && f.ResourceKey == "SharedLib.dll");
            Assert.NotNull(finding);
            Assert.Equal(Impact.High, finding.Impact);
            Assert.Contains("ModA", finding.InvolvedMods);
            Assert.Contains("ModB", finding.InvolvedMods);
        }

        [Fact]
        public void Run_DuplicateModFolder_ReportsCriticalFinding()
        {
            string bepMod = Path.Combine(_gameDir, "BepInEx", "plugins", "DuplicateMod");
            string qmodMod = Path.Combine(_gameDir, "QMods", "DuplicateMod");
            Directory.CreateDirectory(bepMod);
            Directory.CreateDirectory(qmodMod);

            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");
            var analyzer = new FileOverrideAnalyzer();

            analyzer.Run(context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Filesystem" && f.ResourceKey == "DuplicateMod" && f.Impact == Impact.Critical);
            Assert.NotNull(finding);
            Assert.Contains("Duplicate mod folder detected", finding.Explanation);
        }

        [Fact]
        public void Run_LoosePluginShadowingModFolder_ReportsMediumFinding()
        {
            string pluginsDir = Path.Combine(_gameDir, "BepInEx", "plugins");
            string modFolder = Path.Combine(pluginsDir, "MyMod");
            Directory.CreateDirectory(modFolder);
            File.WriteAllBytes(Path.Combine(pluginsDir, "MyMod.dll"), new byte[] { 1, 2, 3 });

            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");
            var analyzer = new FileOverrideAnalyzer();

            analyzer.Run(context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Filesystem" && f.Impact == Impact.Medium);
            Assert.NotNull(finding);
            Assert.Contains("Loose plugin", finding.Explanation);
        }
    }
}
