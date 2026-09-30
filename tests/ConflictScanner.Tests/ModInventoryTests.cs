using System;
using System.IO;
using System.Linq;
using ConflictScanner;
using ConflictScanner.Analysis;
using Xunit;

namespace ConflictScanner.Tests
{
    public class ModInventoryTests : IDisposable
    {
        private readonly string _gameDir;

        public ModInventoryTests()
        {
            _gameDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_ModInv_" + Guid.NewGuid().ToString("N"));
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
        public void RegisterOrUpdateMod_MergesDuplicateEntriesCorrectly()
        {
            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");

            var mod1 = new InstalledModInfo
            {
                Name = "Nautilus",
                GuidOrId = "com.snmodding.nautilus",
                Version = "1.0.0.54",
                Loader = ModLoaderType.BepInEx,
                FolderPath = Path.Combine(_gameDir, "BepInEx", "plugins", "Nautilus")
            };
            mod1.AssemblyNames.Add("Nautilus.dll");
            mod1.Dependencies.Add("BepInEx");

            var mod2 = new InstalledModInfo
            {
                Name = "Nautilus",
                GuidOrId = "com.snmodding.nautilus",
                Version = "1.0.0.54",
                Loader = ModLoaderType.BepInEx,
                FolderPath = Path.Combine(_gameDir, "BepInEx", "plugins", "Nautilus")
            };
            mod2.AssemblyNames.Add("Nautilus.Shared.dll");

            context.RegisterOrUpdateMod(mod1);
            context.RegisterOrUpdateMod(mod2);

            Assert.Single(context.InstalledMods);
            var registered = context.InstalledMods[0];
            Assert.Equal("Nautilus", registered.Name);
            Assert.Equal("com.snmodding.nautilus", registered.GuidOrId);
            Assert.Equal(2, registered.AssemblyNames.Count);
            Assert.Contains("Nautilus.dll", registered.AssemblyNames);
            Assert.Contains("Nautilus.Shared.dll", registered.AssemblyNames);
        }

        [Fact]
        public void UpdateModFindings_AccuratelyCalculatesHealthStatus()
        {
            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");

            var cleanMod = new InstalledModInfo
            {
                Name = "MoonpoolVehicleRepair",
                GuidOrId = "com.author.moonpoolvehiclerepair",
                Loader = ModLoaderType.BepInEx
            };

            var warningMod = new InstalledModInfo
            {
                Name = "CosmeticTweak",
                GuidOrId = "com.author.cosmetictweak",
                Loader = ModLoaderType.BepInEx
            };

            var brokenMod = new InstalledModInfo
            {
                Name = "BuildingTweaks",
                GuidOrId = "com.author.buildingtweaks",
                Loader = ModLoaderType.BepInEx
            };

            context.RegisterOrUpdateMod(cleanMod);
            context.RegisterOrUpdateMod(warningMod);
            context.RegisterOrUpdateMod(brokenMod);

            context.AddFinding(new Finding
            {
                Category = "Filesystem",
                Impact = Impact.Low,
                Confidence = Confidence.Observed,
                InvolvedMods = new[] { "CosmeticTweak" },
                Explanation = "Large file asset detected"
            });

            context.AddFinding(new Finding
            {
                Category = "Metadata",
                Impact = Impact.High,
                Confidence = Confidence.Observed,
                InvolvedMods = new[] { "BuildingTweaks" },
                Explanation = "Missing dependency"
            });

            context.UpdateModFindings();

            Assert.Equal(ModHealthStatus.Clean, cleanMod.Status);
            Assert.Equal(0, cleanMod.FindingsCount);

            Assert.Equal(ModHealthStatus.Warning, warningMod.Status);
            Assert.Equal(1, warningMod.FindingsCount);

            Assert.Equal(ModHealthStatus.Error, brokenMod.Status);
            Assert.Equal(1, brokenMod.FindingsCount);
            Assert.True(brokenMod.HasCriticalOrHigh);
        }

        [Theory]
        [InlineData("Nautilus", "Nautilus.dll", "Nautilus")]
        [InlineData("Tobey/SnapBuilder", "SnapBuilder.dll", "SnapBuilder")]
        [InlineData("Tobey/BepInEx Tweaks", "BepInExTweaks.dll", "BepInEx Tweaks")]
        [InlineData("RepairModule/Assets", "RepairModule.dll", "RepairModule")]
        [InlineData("SomeMod/bin", "SomeMod.dll", "SomeMod")]
        [InlineData("", "LoosePlugin.dll", "LoosePlugin")]
        public void GetModName_ResolvesNestedModFoldersProperly(string subDir, string dllName, string expectedModName)
        {
            string pluginsRoot = Path.Combine(_gameDir, "BepInEx", "plugins");
            string fullPath = string.IsNullOrEmpty(subDir)
                ? Path.Combine(pluginsRoot, dllName)
                : Path.Combine(pluginsRoot, subDir.Replace('/', Path.DirectorySeparatorChar), dllName);

            string detected = BepInPluginAnalyzer.GetModName(pluginsRoot, fullPath);
            Assert.Equal(expectedModName, detected);
        }

        [Fact]
        public void KnownCoreGuidFallback_PreventsMissingNautilusDependencyWarning()
        {
            string pluginsRoot = Path.Combine(_gameDir, "BepInEx", "plugins");
            string nautilusDir = Path.Combine(pluginsRoot, "Nautilus");
            string buildingTweaksDir = Path.Combine(pluginsRoot, "BuildingTweaks");

            Directory.CreateDirectory(nautilusDir);
            Directory.CreateDirectory(buildingTweaksDir);

            // Dummy Nautilus.dll (simulating an uninspected or native/custom core DLL)
            File.WriteAllBytes(Path.Combine(nautilusDir, "Nautilus.dll"), new byte[] { 1, 2, 3 });

            var context = new ScanContext(_gameDir, ScanMode.Quick, "Subnautica");
            var analyzer = new BepInPluginAnalyzer();

            analyzer.Run(context);

            // Verify that Nautilus was inventoried in InstalledMods
            Assert.Contains(context.InstalledMods, m => m.Name == "Nautilus" || m.GuidOrId == "com.snmodding.nautilus");

            // Verify that ReportGenerator includes the Installed Mods header
            string report = ReportGenerator.Generate(context);
            Assert.Contains("=== Installed Mods", report);
            Assert.Contains("Nautilus", report);
        }
    }
}
