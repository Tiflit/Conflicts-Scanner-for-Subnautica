using System.Threading.Tasks;
using ConflictScanner;
using ConflictScanner.ViewModels;
using Xunit;

namespace ConflictScanner.Tests
{
    public class MainWindowViewModelTests
    {
        [Fact]
        public void ApplyFilter_CategoryFiltering_WorksCorrectly()
        {
            var vm = new MainWindowViewModel();
            vm.Findings.Add(new Finding
            {
                Category = "Harmony",
                Impact = Impact.High,
                Confidence = Confidence.Probable,
                Explanation = "Harmony patch collision"
            });
            vm.Findings.Add(new Finding
            {
                Category = "Metadata",
                Impact = Impact.Critical,
                Confidence = Confidence.Observed,
                Explanation = "Duplicate plugin GUID"
            });

            vm.SelectedCategory = "Harmony";

            Assert.Single(vm.FilteredFindings);
            Assert.Equal("Harmony", vm.FilteredFindings[0].Category);

            vm.SelectedCategory = "All";
            Assert.Equal(2, vm.FilteredFindings.Count);
        }

        [Fact]
        public void ApplyFilter_MinImpactFiltering_WorksCorrectly()
        {
            var vm = new MainWindowViewModel();
            vm.Findings.Add(new Finding
            {
                Category = "Harmony",
                Impact = Impact.Critical,
                Confidence = Confidence.Observed,
                Explanation = "Critical issue"
            });
            vm.Findings.Add(new Finding
            {
                Category = "Filesystem",
                Impact = Impact.Medium,
                Confidence = Confidence.Probable,
                Explanation = "Medium issue"
            });
            vm.Findings.Add(new Finding
            {
                Category = "Nautilus",
                Impact = Impact.Low,
                Confidence = Confidence.Heuristic,
                Explanation = "Low issue"
            });

            vm.SelectedMinImpact = "Critical only";
            Assert.Single(vm.FilteredFindings);
            Assert.Equal(Impact.Critical, vm.FilteredFindings[0].Impact);

            vm.SelectedMinImpact = "High+";
            Assert.Single(vm.FilteredFindings); // Only Critical is >= High

            vm.SelectedMinImpact = "Medium+";
            Assert.Equal(2, vm.FilteredFindings.Count); // Critical + Medium
        }

        [Fact]
        public void ApplyFilter_SearchQuery_FiltersByModOrExplanation()
        {
            var vm = new MainWindowViewModel();
            vm.Findings.Add(new Finding
            {
                Category = "Nautilus",
                Impact = Impact.High,
                Confidence = Confidence.Observed,
                InvolvedMods = new[] { "SeamothDepthMod" },
                Explanation = "TechType collision on CustomDepthModule"
            });
            vm.Findings.Add(new Finding
            {
                Category = "Filesystem",
                Impact = Impact.Medium,
                Confidence = Confidence.Probable,
                InvolvedMods = new[] { "QuickSlotsMod" },
                Explanation = "Assembly collision"
            });

            vm.SearchFilter = "Seamoth";
            Assert.Single(vm.FilteredFindings);
            Assert.Equal("SeamothDepthMod", vm.FilteredFindings[0].InvolvedMods[0]);

            vm.SearchFilter = "QuickSlots";
            Assert.Single(vm.FilteredFindings);
            Assert.Equal("QuickSlotsMod", vm.FilteredFindings[0].InvolvedMods[0]);

            vm.ResetFiltersCommand.Execute(null);
            Assert.Equal(2, vm.FilteredFindings.Count);
            Assert.Empty(vm.SearchFilter);
        }

        [Fact]
        public void OpenModFolder_NullOrInvalidPath_DoesNotThrow()
        {
            var vm = new MainWindowViewModel();
            var finding = new Finding
            {
                InvolvedMods = new[] { "NonExistentMod" }
            };

            vm.OpenModFolderCommand.Execute(finding);
            vm.OpenModFolderCommand.Execute(null);
        }

        [Fact]
        public void ApplyModFilter_SearchAndStatusFilter_WorksCorrectly()
        {
            var vm = new MainWindowViewModel();
            var mod1 = new InstalledModInfo
            {
                Name = "Nautilus",
                GuidOrId = "com.snmodding.nautilus",
                Loader = ModLoaderType.BepInEx,
                FindingsCount = 0
            };
            var mod2 = new InstalledModInfo
            {
                Name = "BuildingTweaks",
                GuidOrId = "com.author.buildingtweaks",
                Loader = ModLoaderType.BepInEx,
                FindingsCount = 1,
                HasCriticalOrHigh = true
            };
            var mod3 = new InstalledModInfo
            {
                Name = "LegacyMod",
                GuidOrId = "SameMod",
                Loader = ModLoaderType.QMod,
                FindingsCount = 0
            };

            vm.InstalledMods.Add(mod1);
            vm.InstalledMods.Add(mod2);
            vm.InstalledMods.Add(mod3);

            // Initially all 3
            vm.ModFilter = string.Empty;
            vm.SelectedModStatusFilter = "All";
            Assert.Equal(3, vm.FilteredInstalledMods.Count);

            // Filter by Clean only
            vm.SelectedModStatusFilter = "Clean only";
            Assert.Equal(2, vm.FilteredInstalledMods.Count);
            Assert.Contains(vm.FilteredInstalledMods, m => m.Name == "Nautilus");
            Assert.Contains(vm.FilteredInstalledMods, m => m.Name == "LegacyMod");

            // Filter by With issues
            vm.SelectedModStatusFilter = "With issues";
            Assert.Single(vm.FilteredInstalledMods);
            Assert.Equal("BuildingTweaks", vm.FilteredInstalledMods[0].Name);

            // Filter by text search
            vm.SelectedModStatusFilter = "All";
            vm.ModFilter = "snmodding";
            Assert.Single(vm.FilteredInstalledMods);
            Assert.Equal("Nautilus", vm.FilteredInstalledMods[0].Name);

            // Reset filters
            vm.ResetModFiltersCommand.Execute(null);
            Assert.Empty(vm.ModFilter);
            Assert.Equal("All", vm.SelectedModStatusFilter);
            Assert.Equal(3, vm.FilteredInstalledMods.Count);
        }

        [Fact]
        public void OpenInstalledModFolder_NullOrMissing_DoesNotThrow()
        {
            var vm = new MainWindowViewModel();
            var mod = new InstalledModInfo
            {
                FolderPath = "Z:\\NonExistentFolder12345"
            };

            vm.OpenInstalledModFolderCommand.Execute(mod);
            vm.OpenInstalledModFolderCommand.Execute(null);
        }
    }
}
