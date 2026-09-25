using System;
using System.Collections.Generic;
using System.IO;

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
