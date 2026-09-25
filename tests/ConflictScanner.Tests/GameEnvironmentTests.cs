using System;
using System.IO;
using System.Linq;
using ConflictScanner;
using ConflictScanner.Profiles;
using Xunit;

namespace ConflictScanner.Tests
{
    public class GameEnvironmentTests : IDisposable
    {
        private readonly string _tempDir;

        public GameEnvironmentTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_EnvTest_" + Guid.NewGuid().ToString("N"));
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
        public void GameEnvironmentDetector_EmptyDirectory_ReturnsUnknown()
        {
            var info = GameEnvironmentDetector.Detect(_tempDir);
            Assert.Equal(GameBranch.Unknown, info.Branch);
            Assert.False(info.HasBepInEx);
            Assert.False(info.HasQMods);
        }

        [Fact]
        public void GameEnvironmentDetector_BepInExFolder_DetectsModernBranch()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "BepInEx"));
            var info = GameEnvironmentDetector.Detect(_tempDir);
            Assert.Equal(GameBranch.Modern2_0, info.Branch);
            Assert.True(info.HasBepInEx);
            Assert.False(info.HasQMods);
        }

        [Fact]
        public void GameEnvironmentDetector_QModsFolder_DetectsLegacyBranch()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "QMods"));
            var info = GameEnvironmentDetector.Detect(_tempDir);
            Assert.Equal(GameBranch.Legacy, info.Branch);
            Assert.False(info.HasBepInEx);
            Assert.True(info.HasQMods);
        }

        [Fact]
        public void GameEnvironmentDetector_BothFolders_ReportsBoth()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "BepInEx"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "QMods"));
            var info = GameEnvironmentDetector.Detect(_tempDir);
            Assert.True(info.HasBepInEx);
            Assert.True(info.HasQMods);
        }

        [Fact]
        public void QModAnalyzer_OnModernBranch_EmitsCriticalCompatibilityFinding()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "QMods"));
            var context = new ScanContext(_tempDir, ScanMode.Quick, "Subnautica")
            {
                Environment = new GameEnvironmentInfo
                {
                    Branch = GameBranch.Modern2_0,
                    HasBepInEx = true,
                    HasQMods = true
                }
            };

            var analyzer = new QModAnalyzer();
            analyzer.Run(context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Compatibility" && f.ResourceKey == "QMods_On_Modern_Branch");
            Assert.NotNull(finding);
            Assert.Equal(Impact.Critical, finding.Impact);
            Assert.Equal(Confidence.Observed, finding.Confidence);
        }

        [Fact]
        public void QModAnalyzer_DualLoadersOnUnknownBranch_EmitsHighImpactFinding()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "QMods"));
            var context = new ScanContext(_tempDir, ScanMode.Quick, "Subnautica")
            {
                Environment = new GameEnvironmentInfo
                {
                    Branch = GameBranch.Unknown,
                    HasBepInEx = true,
                    HasQMods = true
                }
            };

            var analyzer = new QModAnalyzer();
            analyzer.Run(context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Compatibility" && f.ResourceKey == "Dual_Mod_Loaders_Detected");
            Assert.NotNull(finding);
            Assert.Equal(Impact.High, finding.Impact);
        }

        [Fact]
        public void QModAnalyzer_DuplicateModId_EmitsCriticalFinding()
        {
            string qmods = Path.Combine(_tempDir, "QMods");
            string mod1 = Path.Combine(qmods, "ModOne");
            string mod2 = Path.Combine(qmods, "ModTwo");
            Directory.CreateDirectory(mod1);
            Directory.CreateDirectory(mod2);

            File.WriteAllText(Path.Combine(mod1, "mod.json"), "{\"Id\": \"DuplicateMod\", \"DisplayName\": \"Mod 1\"}");
            File.WriteAllText(Path.Combine(mod2, "mod.json"), "{\"Id\": \"DuplicateMod\", \"DisplayName\": \"Mod 2\"}");

            var context = new ScanContext(_tempDir, ScanMode.Quick, "Subnautica");
            var analyzer = new QModAnalyzer();
            analyzer.Run(context);

            var dupFinding = context.Findings.FirstOrDefault(f => f.Category == "QMod" && f.ResourceKey == "DuplicateMod");
            Assert.NotNull(dupFinding);
            Assert.Equal(Impact.Critical, dupFinding.Impact);
            Assert.Equal(2, dupFinding.InvolvedMods.Count);
        }

        [Fact]
        public void QModAnalyzer_MissingDependency_EmitsHighImpactFinding()
        {
            string qmods = Path.Combine(_tempDir, "QMods");
            string mod1 = Path.Combine(qmods, "DependentMod");
            Directory.CreateDirectory(mod1);

            File.WriteAllText(Path.Combine(mod1, "mod.json"), "{\"Id\": \"DependentMod\", \"Dependencies\": [\"NonExistentPrereq\"]}");

            var context = new ScanContext(_tempDir, ScanMode.Quick, "Subnautica");
            var analyzer = new QModAnalyzer();
            analyzer.Run(context);

            var missFinding = context.Findings.FirstOrDefault(f => f.Category == "QMod" && f.ResourceKey == "NonExistentPrereq");
            Assert.NotNull(missFinding);
            Assert.Equal(Impact.High, missFinding.Impact);
            Assert.Contains("DependentMod", missFinding.InvolvedMods);
        }
    }
}
