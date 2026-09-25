using System;
using System.Linq;
using ConflictScanner;
using ConflictScanner.Analysis;
using Xunit;

namespace ConflictScanner.Tests
{
    public class HarmonyAnalyzerTests
    {
        [Fact]
        public void AnalyzePatches_MultipleTranspilersSameTarget_EmitsHighImpactFinding()
        {
            var patch1 = new HarmonyPatchTarget("ModA", "Player", "Awake", "Transpiler", 400);
            var patch2 = new HarmonyPatchTarget("ModB", "Player", "Awake", "Transpiler", 400);

            var context = new ScanContext("dummy", ScanMode.Quick, "Subnautica");
            HarmonyAnalyzer.AnalyzePatches(new[] { patch1, patch2 }, context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Harmony" && f.Impact == Impact.High);
            Assert.NotNull(finding);
            Assert.Contains("ModA", finding.InvolvedMods);
            Assert.Contains("ModB", finding.InvolvedMods);
            Assert.Equal("Player.Awake", finding.ResourceKey);
        }

        [Fact]
        public void AnalyzePatches_PrefixPriorityTie_EmitsMediumImpactFinding()
        {
            var patch1 = new HarmonyPatchTarget("ModA", "Player", "Update", "Prefix", 400);
            var patch2 = new HarmonyPatchTarget("ModB", "Player", "Update", "Prefix", 400);

            var context = new ScanContext("dummy", ScanMode.Quick, "Subnautica");
            HarmonyAnalyzer.AnalyzePatches(new[] { patch1, patch2 }, context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Harmony" && f.Impact == Impact.Medium);
            Assert.NotNull(finding);
            Assert.Contains("priority tie", finding.Explanation, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyzePatches_PrefixSuppressionWithBooleanReturn_EmitsSuppressionFinding()
        {
            var patch1 = new HarmonyPatchTarget("ModA", "Player", "CanBreathe", "Prefix", 500, ReturnsBoolean: true);
            var patch2 = new HarmonyPatchTarget("ModB", "Player", "CanBreathe", "Prefix", 400, ReturnsBoolean: false);

            var context = new ScanContext("dummy", ScanMode.Quick, "Subnautica");
            HarmonyAnalyzer.AnalyzePatches(new[] { patch1, patch2 }, context);

            var finding = context.Findings.FirstOrDefault(f => f.Category == "Harmony" && f.Explanation.Contains("prefix suppression", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(finding);
            Assert.Contains("ModA", finding.Evidence);
        }

        [Fact]
        public void AnalyzePatches_DifferentTargets_NoFindings()
        {
            var patch1 = new HarmonyPatchTarget("ModA", "Player", "Awake", "Prefix", 400);
            var patch2 = new HarmonyPatchTarget("ModB", "Crafter", "Craft", "Prefix", 400);

            var context = new ScanContext("dummy", ScanMode.Quick, "Subnautica");
            HarmonyAnalyzer.AnalyzePatches(new[] { patch1, patch2 }, context);

            Assert.Empty(context.Findings);
        }
    }
}
