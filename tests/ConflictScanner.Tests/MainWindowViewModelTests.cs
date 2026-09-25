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
    }
}
