using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConflictScanner
{
    public class QModAnalyzer
    {
        private class QModManifest
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Version { get; set; }
            public string[] Dependencies { get; set; }
        }

        public void Run(ScanContext context)
        {
            string qmodsPath = Path.Combine(context.GamePath, "QMods");

            if (!Directory.Exists(qmodsPath))
            {
                context.QModWarnings.Add("No QMods folder found.");
                return;
            }

            var manifests = new List<(string Folder, QModManifest Manifest)>();
            var idCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var modFolder in Directory.GetDirectories(qmodsPath))
            {
                string manifestPath = Path.Combine(modFolder, "mod.json");

                if (!File.Exists(manifestPath))
                {
                    context.QModWarnings.Add($"[{Path.GetFileName(modFolder)}] Missing mod.json.");
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<QModManifest>(json);

                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
                    {
                        context.QModWarnings.Add($"[{Path.GetFileName(modFolder)}] Invalid or missing mod ID.");
                        continue;
                    }

                    manifests.Add((modFolder, manifest));

                    if (!idCounts.ContainsKey(manifest.Id))
                        idCounts[manifest.Id] = 0;

                    idCounts[manifest.Id]++;
                }
                catch (Exception ex)
                {
                    context.QModWarnings.Add($"[{Path.GetFileName(modFolder)}] Failed to parse mod.json: {ex.Message}");
                }
            }

            // Detect duplicate IDs
            foreach (var pair in idCounts)
            {
                if (pair.Value > 1)
                    context.QModWarnings.Add($"Duplicate QMod ID detected: \"{pair.Key}\" appears {pair.Value} times.");
            }

            // Detect missing dependencies
            foreach (var (folder, manifest) in manifests)
            {
                if (manifest.Dependencies == null)
                    continue;

                foreach (var dep in manifest.Dependencies)
                {
                    bool exists = manifests.Exists(m => 
                        m.Manifest.Id.Equals(dep, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                        context.QModWarnings.Add(
                            $"[{manifest.Id}] Missing dependency: \"{dep}\""
                        );
                }
            }
        }
    }
}
