using System.Collections.Generic;
using ConflictScanner;
using Xunit;

namespace ConflictScanner.Tests
{
    public class SuggestionEngineTests
    {
        [Fact]
        public void Generate_EmptyFindings_ReturnsHealthyMessage()
        {
            var context = new ScanContext("fake_path", ScanMode.Quick, "Subnautica");
            SuggestionEngine.Generate(context);

            Assert.Single(context.Suggestions);
            Assert.Contains("healthy", context.Suggestions[0]);
        }

        [Fact]
        public void Generate_CriticalFinding_EmitsCriticalSuggestion()
        {
            var context = new ScanContext("fake_path", ScanMode.Quick, "Subnautica");
            context.AddFinding(new Finding
            {
                Category = "Metadata",
                Impact = Impact.Critical,
                Confidence = Confidence.Observed,
                InvolvedMods = new[] { "ModA", "ModB" },
                Explanation = "Duplicate BepInPlugin GUID detected: \"com.test.mod\""
            });

            SuggestionEngine.Generate(context);

            Assert.Contains(context.Suggestions, s => s.StartsWith("CRITICAL:"));
            Assert.Contains(context.Suggestions, s => s.StartsWith("DUPLICATE GUIDS:"));
        }

        [Fact]
        public void Generate_HarmonyTranspilerConflict_EmitsHarmonySuggestion()
        {
            var context = new ScanContext("fake_path", ScanMode.Quick, "Subnautica");
            context.AddFinding(new Finding
            {
                Category = "Harmony",
                Impact = Impact.High,
                Confidence = Confidence.Probable,
                InvolvedMods = new[] { "ModA", "ModB" },
                Explanation = "Multiple mods apply IL transpilers to Player.Update"
            });

            SuggestionEngine.Generate(context);

            Assert.Contains(context.Suggestions, s => s.StartsWith("HARMONY COLLISION:"));
        }
    }
}
