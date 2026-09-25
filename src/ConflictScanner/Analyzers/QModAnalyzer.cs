using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ConflictScanner.Profiles;

namespace ConflictScanner
{
    public class QModAnalyzer : IAnalyzer
    {
        private class QModManifest
        {
            public string? Id { get; set; }
            public string? DisplayName { get; set; }
            public string? Version { get; set; }
            public string[]? Dependencies { get; set; }
        }

        public void Run(ScanContext context)
        {
            string qmodsPath = Path.Combine(context.GamePath, "QMods");

            if (!Directory.Exists(qmodsPath))
            {
                context.AddQModWarning(Severity.Info, "No QMods folder found.");
                return;
            }

            var env = context.Environment ?? GameEnvironmentDetector.Detect(context.GamePath);

            // 1. Compatibility check: Subnautica 2.0+ (Living Large) vs QMods
            if (env.Branch == GameBranch.Modern2_0)
            {
                context.AddFinding(new Finding
                {
                    Category = "Compatibility",
                    Impact = Impact.Critical,
                    Confidence = Confidence.Observed,
                    ResourceKey = "QMods_On_Modern_Branch",
                    Evidence = "QMods folder present on Subnautica 2.0+ (Living Large)",
                    Explanation = "Subnautica 2.0+ (Living Large) uses BepInEx and Nautilus. QModManager and legacy QMods are fundamentally incompatible and will not load or will cause startup crashes.",
                    SuggestedAction = "Migrate your mods to modern BepInEx/Nautilus versions or revert your game to the Steam 'legacy - Early 2021' branch."
                });
            }
            else if (env.HasBepInEx)
            {
                // Both BepInEx and QMods present in non-2.0 or undetermined environment
                context.AddFinding(new Finding
                {
                    Category = "Compatibility",
                    Impact = Impact.High,
                    Confidence = Confidence.Observed,
                    ResourceKey = "Dual_Mod_Loaders_Detected",
                    Evidence = "Both BepInEx/ and QMods/ directories are present in the game installation.",
                    Explanation = "Both BepInEx and QModManager are installed simultaneously. Having two distinct mod loaders active can cause duplicate patching, memory corruption, and unpredictable startup errors.",
                    SuggestedAction = "Keep only one mod loader: BepInEx for Subnautica 2.0+ or QModManager for Legacy Subnautica."
                });
            }

            var manifests = new List<(string Folder, QModManifest Manifest)>();
            var idCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var modFolder in Directory.GetDirectories(qmodsPath))
            {
                string manifestPath = Path.Combine(modFolder, "mod.json");

                if (!File.Exists(manifestPath))
                {
                    context.AddQModWarning(
                        Severity.Warning,
                        $"[{Path.GetFileName(modFolder)}] Missing mod.json."
                    );
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<QModManifest>(json);

                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
                    {
                        context.AddQModWarning(
                            Severity.Warning,
                            $"[{Path.GetFileName(modFolder)}] Invalid or missing mod ID."
                        );
                        continue;
                    }

                    manifests.Add((modFolder, manifest));

                    if (!idCounts.ContainsKey(manifest.Id))
                        idCounts[manifest.Id] = 0;

                    idCounts[manifest.Id]++;
                }
                catch (Exception ex)
                {
                    context.AddQModWarning(
                        Severity.Warning,
                        $"[{Path.GetFileName(modFolder)}] Failed to parse mod.json: {ex.Message}"
                    );
                }
            }

            // 2. Duplicate QMod IDs
            foreach (var pair in idCounts)
            {
                if (pair.Value > 1)
                {
                    var modsWithId = manifests
                        .Where(m => m.Manifest.Id != null && m.Manifest.Id.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(m => Path.GetFileName(m.Folder))
                        .ToList();

                    context.AddFinding(new Finding
                    {
                        Category = "QMod",
                        Impact = Impact.Critical,
                        Confidence = Confidence.Observed,
                        InvolvedMods = modsWithId,
                        ResourceKey = pair.Key,
                        Evidence = $"QMod ID \"{pair.Key}\" found in folders: {string.Join(", ", modsWithId)}",
                        Explanation = $"Duplicate QMod ID detected: \"{pair.Key}\" is declared by multiple mods. QModManager cannot load multiple mods with identical IDs.",
                        SuggestedAction = "Remove the duplicate or older version of this mod."
                    });
                }
            }

            // 3. Missing QMod Dependencies
            foreach (var (folder, manifest) in manifests)
            {
                if (manifest.Dependencies == null || manifest.Id == null)
                    continue;

                foreach (var dep in manifest.Dependencies)
                {
                    bool exists = manifests.Exists(m =>
                        m.Manifest.Id != null && m.Manifest.Id.Equals(dep, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        context.AddFinding(new Finding
                        {
                            Category = "QMod",
                            Impact = Impact.High,
                            Confidence = Confidence.Observed,
                            InvolvedMods = new[] { manifest.Id },
                            ResourceKey = dep,
                            Evidence = $"[{manifest.Id}] requires QMod dependency \"{dep}\"",
                            Explanation = $"Missing required dependency: QMod \"{manifest.Id}\" depends on \"{dep}\", which was not found in the QMods folder.",
                            SuggestedAction = $"Download and install the required QMod \"{dep}\"."
                        });
                    }
                }
            }
        }
    }
}
