using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConflictScanner.Analysis;

namespace ConflictScanner
{
    /// <summary>
    /// Static Harmony patch analyzer powered by Mono.Cecil.
    /// Inspects class-level and method-level Harmony patches across mod assemblies
    /// to detect transpiler collisions, prefix priority conflicts, and prefix suppression.
    /// </summary>
    public class HarmonyAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            string bepPlugins = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPlugins))
                return;

            var allPatches = new List<HarmonyPatchTarget>();

            foreach (var dll in Directory.GetFiles(bepPlugins, "*.dll", SearchOption.AllDirectories))
            {
                string modName = GetModName(bepPlugins, dll);
                var result = CecilAssemblyReader.AnalyzeAssembly(dll, modName);
                if (result == null || result.HarmonyPatches.Count == 0)
                    continue;

                allPatches.AddRange(result.HarmonyPatches);
            }

            AnalyzePatches(allPatches, context);
        }

        public static void AnalyzePatches(IReadOnlyList<HarmonyPatchTarget> allPatches, ScanContext context)
        {
            if (allPatches == null || allPatches.Count == 0)
                return;

            var targetGroups = allPatches.GroupBy(p => $"{p.TargetTypeName}.{p.TargetMethodName}", StringComparer.OrdinalIgnoreCase);

            foreach (var group in targetGroups)
            {
                string targetName = group.Key;
                var patchesOnTarget = group.ToList();

                // 1. Multiple Transpilers on the same method (High Impact)
                var transpilers = patchesOnTarget.Where(p => p.PatchType.Equals("Transpiler", StringComparison.OrdinalIgnoreCase)).ToList();
                var distinctTranspilerMods = transpilers.Select(t => t.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (distinctTranspilerMods.Count > 1)
                {
                    context.AddFinding(new Finding
                    {
                        Category = "Harmony",
                        Impact = Impact.High,
                        Confidence = Confidence.Probable,
                        InvolvedMods = distinctTranspilerMods,
                        ResourceKey = targetName,
                        Evidence = $"Transpiler patches on {targetName} by: {string.Join(", ", distinctTranspilerMods)}",
                        Explanation = $"Multiple mods apply IL transpilers to \"{targetName}\". Concurrent transpilers modifying the same instruction stream frequently collide, causing patches to fail or produce runtime exceptions.",
                        SuggestedAction = "Inspect these mods for compatibility or contact the mod authors."
                    });
                }

                // 2. Prefix priority conflicts (Medium Impact)
                var prefixes = patchesOnTarget.Where(p => p.PatchType.Equals("Prefix", StringComparison.OrdinalIgnoreCase)).ToList();
                var distinctPrefixMods = prefixes.Select(p => p.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (distinctPrefixMods.Count > 1)
                {
                    var ordered = prefixes.OrderBy(p => p.Priority).ToList();
                    bool priorityTied = false;

                    for (int i = 0; i < ordered.Count - 1; i++)
                    {
                        if (ordered[i].Priority == ordered[i + 1].Priority &&
                            !ordered[i].ModName.Equals(ordered[i + 1].ModName, StringComparison.OrdinalIgnoreCase))
                        {
                            priorityTied = true;
                            break;
                        }
                    }

                    if (priorityTied)
                    {
                        context.AddFinding(new Finding
                        {
                            Category = "Harmony",
                            Impact = Impact.Medium,
                            Confidence = Confidence.Probable,
                            InvolvedMods = distinctPrefixMods,
                            ResourceKey = targetName,
                            Evidence = $"Prefixes on {targetName}: {string.Join(", ", prefixes.Select(p => $"{p.ModName} (priority {p.Priority})"))}",
                            Explanation = $"Prefix priority tie on \"{targetName}\". Multiple mods attach prefix patches with the same priority, meaning their order of execution is undefined.",
                            SuggestedAction = "If one mod depends on running before or after the other, set explicit HarmonyPriority values."
                        });
                    }

                    // 3. Prefix suppression check (Prefix returning boolean)
                    var boolPrefixes = prefixes.Where(p => p.ReturnsBoolean).ToList();
                    if (boolPrefixes.Count > 0)
                    {
                        var boolMods = boolPrefixes.Select(p => p.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        context.AddFinding(new Finding
                        {
                            Category = "Harmony",
                            Impact = boolPrefixes.Count > 1 ? Impact.High : Impact.Medium,
                            Confidence = Confidence.Probable,
                            InvolvedMods = distinctPrefixMods,
                            ResourceKey = targetName,
                            Evidence = $"Suppressible prefix(es) on {targetName} by: {string.Join(", ", boolMods)}",
                            Explanation = $"Potential prefix suppression on \"{targetName}\". Mod(s) {string.Join(", ", boolMods)} use boolean prefix patches that can cancel the original method and prevent other mods' patches from executing.",
                            SuggestedAction = "Verify if these mods alter the same behavior, as one may suppress the other depending on execution order."
                        });
                    }
                }
            }
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
