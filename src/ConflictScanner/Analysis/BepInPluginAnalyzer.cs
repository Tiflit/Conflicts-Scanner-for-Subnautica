using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConflictScanner.Analysis
{
    public class BepInPluginAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            string bepPlugins = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPlugins))
                return;

            var plugins = new List<BepInPluginInfo>();
            var guidMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var dll in Directory.GetFiles(bepPlugins, "*.dll", SearchOption.AllDirectories))
            {
                string modName = GetModName(bepPlugins, dll);
                var result = CecilAssemblyReader.AnalyzeAssembly(dll, modName);
                if (result == null)
                    continue;

                foreach (var plugin in result.Plugins)
                {
                    plugins.Add(plugin);

                    if (!guidMap.ContainsKey(plugin.Guid))
                        guidMap[plugin.Guid] = new List<string>();

                    guidMap[plugin.Guid].Add(plugin.ModName);
                }
            }

            // 1. Detect duplicate GUIDs
            foreach (var (guid, mods) in guidMap)
            {
                if (mods.Count > 1)
                {
                    context.AddFinding(new Finding
                    {
                        Category = "Metadata",
                        Impact = Impact.Critical,
                        Confidence = Confidence.Observed,
                        InvolvedMods = mods,
                        ResourceKey = guid,
                        Evidence = $"BepInPlugin GUID \"{guid}\" declared in: {string.Join(", ", mods)}",
                        Explanation = $"Duplicate BepInPlugin GUID detected: \"{guid}\". BepInEx will reject or fail to load duplicate plugin GUIDs.",
                        SuggestedAction = "Remove the duplicate or outdated version of this mod."
                    });
                }
            }

            // 2. Detect missing dependencies
            foreach (var plugin in plugins)
            {
                foreach (var dep in plugin.Dependencies)
                {
                    if (!guidMap.ContainsKey(dep.TargetGuid))
                    {
                        var impact = dep.IsHardDependency ? Impact.High : Impact.Low;
                        var typeDesc = dep.IsHardDependency ? "Required dependency" : "Optional dependency";

                        context.AddFinding(new Finding
                        {
                            Category = "Metadata",
                            Impact = impact,
                            Confidence = Confidence.Observed,
                            InvolvedMods = new[] { plugin.ModName },
                            ResourceKey = dep.TargetGuid,
                            Evidence = $"[{plugin.ModName}] declares {typeDesc} on \"{dep.TargetGuid}\"",
                            Explanation = $"Missing {typeDesc.ToLowerInvariant()}: \"{dep.TargetGuid}\" is needed by \"{plugin.ModName}\" but was not found in BepInEx/plugins.",
                            SuggestedAction = $"Install the required mod with GUID \"{dep.TargetGuid}\"."
                        });
                    }
                }
            }

            // 3. Detect circular dependencies
            var cycleFindings = DetectCircularDependencies(plugins, guidMap);
            foreach (var finding in cycleFindings)
            {
                context.AddFinding(finding);
            }
        }

        public static List<Finding> DetectCircularDependencies(
            IReadOnlyList<BepInPluginInfo> plugins,
            IReadOnlyDictionary<string, List<string>> guidMap)
        {
            var findings = new List<Finding>();
            if (plugins == null || plugins.Count == 0)
                return findings;

            // Build adjacency map: GUID -> set of target GUIDs
            var adj = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var pluginModMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var plugin in plugins)
            {
                if (!adj.ContainsKey(plugin.Guid))
                    adj[plugin.Guid] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!pluginModMap.ContainsKey(plugin.Guid))
                    pluginModMap[plugin.Guid] = plugin.ModName;

                foreach (var dep in plugin.Dependencies)
                {
                    if (guidMap.ContainsKey(dep.TargetGuid))
                    {
                        adj[plugin.Guid].Add(dep.TargetGuid);
                    }
                }
            }

            var visitState = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=unvisited, 1=visiting, 2=visited
            var currentPath = new List<string>();
            var reportedCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Dfs(string current)
            {
                visitState[current] = 1;
                currentPath.Add(current);

                if (adj.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        visitState.TryGetValue(neighbor, out int neighborState);
                        if (neighborState == 1)
                        {
                            // Cycle detected
                            int startIndex = currentPath.FindIndex(g => g.Equals(neighbor, StringComparison.OrdinalIgnoreCase));
                            if (startIndex >= 0)
                            {
                                var cycle = currentPath.Skip(startIndex).ToList();
                                string canonicalKey = GetCanonicalCycleKey(cycle);
                                if (reportedCycles.Add(canonicalKey))
                                {
                                    findings.Add(CreateCycleFinding(cycle, pluginModMap, guidMap));
                                }
                            }
                        }
                        else if (neighborState == 0)
                        {
                            Dfs(neighbor);
                        }
                    }
                }

                currentPath.RemoveAt(currentPath.Count - 1);
                visitState[current] = 2;
            }

            foreach (var node in adj.Keys)
            {
                if (!visitState.TryGetValue(node, out int st) || st == 0)
                {
                    Dfs(node);
                }
            }

            return findings;
        }

        private static string GetCanonicalCycleKey(List<string> cycle)
        {
            if (cycle.Count == 0)
                return string.Empty;

            int minIdx = 0;
            for (int i = 1; i < cycle.Count; i++)
            {
                if (string.Compare(cycle[i], cycle[minIdx], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    minIdx = i;
                }
            }

            var rotated = cycle.Skip(minIdx).Concat(cycle.Take(minIdx));
            return string.Join("->", rotated.Select(g => g.ToLowerInvariant()));
        }

        private static Finding CreateCycleFinding(
            List<string> cycleGuids,
            IReadOnlyDictionary<string, string> pluginModMap,
            IReadOnlyDictionary<string, List<string>> guidMap)
        {
            var involvedMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var chainParts = new List<string>();

            foreach (var guid in cycleGuids)
            {
                string modName = pluginModMap.TryGetValue(guid, out var m) ? m : guid;
                if (guidMap.TryGetValue(guid, out var mList) && mList.Count > 0)
                    modName = mList[0];

                involvedMods.Add(modName);
                chainParts.Add($"{modName} ({guid})");
            }

            // Close the loop in display chain
            string firstGuid = cycleGuids[0];
            string firstMod = pluginModMap.TryGetValue(firstGuid, out var fm) ? fm : firstGuid;
            if (guidMap.TryGetValue(firstGuid, out var fList) && fList.Count > 0)
                firstMod = fList[0];
            chainParts.Add($"{firstMod} ({firstGuid})");

            string chainDisplay = string.Join(" -> ", chainParts);

            return new Finding
            {
                Category = "Dependencies",
                Impact = Impact.Critical,
                Confidence = Confidence.Observed,
                InvolvedMods = involvedMods.ToList(),
                ResourceKey = "Cycle:" + string.Join("->", cycleGuids),
                Evidence = $"Circular dependency chain: {chainDisplay}",
                Explanation = $"Circular dependency detected: {chainDisplay}. BepInEx cannot determine load order for cyclic dependencies and will fail to load these plugins.",
                SuggestedAction = "Remove or update one of the conflicting mods to break the dependency cycle."
            };
        }

        private static string GetModName(string pluginsRoot, string dllPath)
        {
            string relative = Path.GetRelativePath(pluginsRoot, dllPath);
            int slashIndex = relative.IndexOf(Path.DirectorySeparatorChar);
            if (slashIndex > 0)
                return relative[..slashIndex];

            return Path.GetFileNameWithoutExtension(dllPath);
        }
    }
}
