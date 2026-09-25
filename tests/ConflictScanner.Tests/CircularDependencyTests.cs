using System;
using System.Collections.Generic;
using System.Linq;
using ConflictScanner;
using ConflictScanner.Analysis;
using Xunit;

namespace ConflictScanner.Tests
{
    public class CircularDependencyTests
    {
        [Fact]
        public void DetectCircularDependencies_AcyclicChain_ReturnsEmpty()
        {
            var pluginA = new BepInPluginInfo("ModA", "ModA.dll", "com.author.moda", "Mod A", "1.0",
                new[] { new BepInDependencyInfo("com.author.modb", true) });
            var pluginB = new BepInPluginInfo("ModB", "ModB.dll", "com.author.modb", "Mod B", "1.0",
                new[] { new BepInDependencyInfo("com.author.modc", true) });
            var pluginC = new BepInPluginInfo("ModC", "ModC.dll", "com.author.modc", "Mod C", "1.0",
                Array.Empty<BepInDependencyInfo>());

            var plugins = new[] { pluginA, pluginB, pluginC };
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["com.author.moda"] = new() { "ModA" },
                ["com.author.modb"] = new() { "ModB" },
                ["com.author.modc"] = new() { "ModC" }
            };

            var findings = BepInPluginAnalyzer.DetectCircularDependencies(plugins, guidMap);
            Assert.Empty(findings);
        }

        [Fact]
        public void DetectCircularDependencies_DirectTwoNodeCycle_DetectsCriticalFinding()
        {
            var pluginA = new BepInPluginInfo("ModA", "ModA.dll", "com.author.moda", "Mod A", "1.0",
                new[] { new BepInDependencyInfo("com.author.modb", true) });
            var pluginB = new BepInPluginInfo("ModB", "ModB.dll", "com.author.modb", "Mod B", "1.0",
                new[] { new BepInDependencyInfo("com.author.moda", true) });

            var plugins = new[] { pluginA, pluginB };
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["com.author.moda"] = new() { "ModA" },
                ["com.author.modb"] = new() { "ModB" }
            };

            var findings = BepInPluginAnalyzer.DetectCircularDependencies(plugins, guidMap);

            Assert.Single(findings);
            var finding = findings[0];
            Assert.Equal(Impact.Critical, finding.Impact);
            Assert.Equal(Confidence.Observed, finding.Confidence);
            Assert.Equal("Dependencies", finding.Category);
            Assert.Contains("ModA", finding.InvolvedMods);
            Assert.Contains("ModB", finding.InvolvedMods);
            Assert.Contains("com.author.moda", finding.Evidence);
            Assert.Contains("com.author.modb", finding.Evidence);
        }

        [Fact]
        public void DetectCircularDependencies_ThreeNodeCycle_DetectsSingleFinding()
        {
            var pluginA = new BepInPluginInfo("ModA", "ModA.dll", "com.author.moda", "Mod A", "1.0",
                new[] { new BepInDependencyInfo("com.author.modb", true) });
            var pluginB = new BepInPluginInfo("ModB", "ModB.dll", "com.author.modb", "Mod B", "1.0",
                new[] { new BepInDependencyInfo("com.author.modc", true) });
            var pluginC = new BepInPluginInfo("ModC", "ModC.dll", "com.author.modc", "Mod C", "1.0",
                new[] { new BepInDependencyInfo("com.author.moda", true) });

            var plugins = new[] { pluginA, pluginB, pluginC };
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["com.author.moda"] = new() { "ModA" },
                ["com.author.modb"] = new() { "ModB" },
                ["com.author.modc"] = new() { "ModC" }
            };

            var findings = BepInPluginAnalyzer.DetectCircularDependencies(plugins, guidMap);

            Assert.Single(findings);
            var finding = findings[0];
            Assert.Equal(Impact.Critical, finding.Impact);
            Assert.Equal(3, finding.InvolvedMods.Count);
        }

        [Fact]
        public void DetectCircularDependencies_SelfDependency_DetectsCycle()
        {
            var pluginA = new BepInPluginInfo("ModA", "ModA.dll", "com.author.moda", "Mod A", "1.0",
                new[] { new BepInDependencyInfo("com.author.moda", true) });

            var plugins = new[] { pluginA };
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["com.author.moda"] = new() { "ModA" }
            };

            var findings = BepInPluginAnalyzer.DetectCircularDependencies(plugins, guidMap);

            Assert.Single(findings);
            var finding = findings[0];
            Assert.Equal(Impact.Critical, finding.Impact);
            Assert.Contains("ModA", finding.InvolvedMods);
        }

        [Fact]
        public void DetectCircularDependencies_ExternalMissingDependency_DoesNotFormFalseCycle()
        {
            // ModA depends on ModB, ModB depends on ModExternal (which is missing / not in guidMap)
            var pluginA = new BepInPluginInfo("ModA", "ModA.dll", "com.author.moda", "Mod A", "1.0",
                new[] { new BepInDependencyInfo("com.author.modb", true) });
            var pluginB = new BepInPluginInfo("ModB", "ModB.dll", "com.author.modb", "Mod B", "1.0",
                new[] { new BepInDependencyInfo("com.author.missing", true) });

            var plugins = new[] { pluginA, pluginB };
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["com.author.moda"] = new() { "ModA" },
                ["com.author.modb"] = new() { "ModB" }
            };

            var findings = BepInPluginAnalyzer.DetectCircularDependencies(plugins, guidMap);
            Assert.Empty(findings);
        }
    }
}
