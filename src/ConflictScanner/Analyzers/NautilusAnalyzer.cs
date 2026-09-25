using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConflictScanner.Analysis;

namespace ConflictScanner
{
    /// <summary>
    /// Static Nautilus analyzer powered by Mono.Cecil.
    /// Inspects modern and legacy Nautilus/SMLHelper API calls across mod assemblies
    /// to detect duplicate TechType IDs, CraftTree collisions, and Sprite key conflicts.
    /// </summary>
    public class NautilusAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            string bepPlugins = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPlugins))
                return;

            var registrations = new List<NautilusRegistration>();

            foreach (var dll in Directory.GetFiles(bepPlugins, "*.dll", SearchOption.AllDirectories))
            {
                string modName = GetModName(bepPlugins, dll);
                var result = CecilAssemblyReader.AnalyzeAssembly(dll, modName);
                if (result == null || result.NautilusRegistrations.Count == 0)
                    continue;

                registrations.AddRange(result.NautilusRegistrations);
            }

            if (registrations.Count == 0)
                return;

            // 1. TechType ID Collisions (High Impact)
            var techTypes = registrations.Where(r => r.Type == "TechType").GroupBy(r => r.Identifier, StringComparer.OrdinalIgnoreCase);
            foreach (var group in techTypes)
            {
                var distinctMods = group.Select(r => r.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (distinctMods.Count > 1)
                {
                    context.AddFinding(new Finding
                    {
                        Category = "Nautilus",
                        Impact = Impact.High,
                        Confidence = Confidence.Observed,
                        InvolvedMods = distinctMods,
                        ResourceKey = group.Key,
                        Evidence = $"TechType \"{group.Key}\" registered by mods: {string.Join(", ", distinctMods)}",
                        Explanation = $"Duplicate TechType identifier detected: \"{group.Key}\". In Subnautica, two mods registering the same TechType string name will collide in the enum registry, causing one mod's items or recipes to overwrite or break the other.",
                        SuggestedAction = "Check whether these mods are alternative versions of each other or rename the custom TechType."
                    });
                }
            }

            // 2. CraftTree Path Collisions (Medium Impact)
            var craftTrees = registrations.Where(r => r.Type == "CraftTree").GroupBy(r => r.Identifier, StringComparer.OrdinalIgnoreCase);
            foreach (var group in craftTrees)
            {
                var distinctMods = group.Select(r => r.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (distinctMods.Count > 1)
                {
                    context.AddFinding(new Finding
                    {
                        Category = "Nautilus",
                        Impact = Impact.Medium,
                        Confidence = Confidence.Probable,
                        InvolvedMods = distinctMods,
                        ResourceKey = group.Key,
                        Evidence = $"CraftTree path \"{group.Key}\" modified by mods: {string.Join(", ", distinctMods)}",
                        Explanation = $"Multiple mods add custom nodes to the exact same CraftTree path: \"{group.Key}\". This may cause UI overlapping or crafting menu glitches depending on load order.",
                        SuggestedAction = "Review crafting menu layout in-game to verify whether both recipes appear correctly."
                    });
                }
            }

            // 3. Sprite Key Collisions (Low Impact)
            var sprites = registrations.Where(r => r.Type == "Sprite").GroupBy(r => r.Identifier, StringComparer.OrdinalIgnoreCase);
            foreach (var group in sprites)
            {
                var distinctMods = group.Select(r => r.ModName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (distinctMods.Count > 1)
                {
                    context.AddFinding(new Finding
                    {
                        Category = "Nautilus",
                        Impact = Impact.Low,
                        Confidence = Confidence.Probable,
                        InvolvedMods = distinctMods,
                        ResourceKey = group.Key,
                        Evidence = $"Sprite key \"{group.Key}\" registered by mods: {string.Join(", ", distinctMods)}",
                        Explanation = $"Multiple mods register a custom sprite with the key \"{group.Key}\". One sprite texture will overwrite the other in the sprite atlas.",
                        SuggestedAction = "Usually cosmetic; verify icons in-game."
                    });
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
