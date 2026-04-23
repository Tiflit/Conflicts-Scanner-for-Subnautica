using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConflictScanner
{
    public class NautilusAnalyzer
    {
        private class NautilusEntry
        {
            public string Id { get; set; }
            public string TechType { get; set; }
        }

        public void Run(ScanContext context)
        {
            string bepPath = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPath))
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    "No BepInEx/plugins folder found. Skipping Nautilus analysis."
                );
                return;
            }

            var idMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var techTypeMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var modFolder in Directory.GetDirectories(bepPath))
            {
                string modName = Path.GetFileName(modFolder);

                foreach (var file in Directory.GetFiles(modFolder, "*.json", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(modFolder.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          .Replace('\\', '/');

                    if (!relative.Contains("nautilus", StringComparison.OrdinalIgnoreCase) &&
                        !relative.Contains("config", StringComparison.OrdinalIgnoreCase) &&
                        !relative.Contains("configs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    TryParseConfig(file, modName, idMap, techTypeMap, context);
                }
            }

            foreach (var pair in idMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.AddNautilusWarning(
                        Severity.Error,
                        $"Duplicate Nautilus Id detected: \"{pair.Key}\" used by mods: {mods}"
                    );
                }
            }

            foreach (var pair in techTypeMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.AddNautilusWarning(
                        Severity.Error,
                        $"Duplicate Nautilus TechType detected: \"{pair.Key}\" used by mods: {mods}"
                    );
                }
            }
        }

        private void TryParseConfig(
            string filePath,
            string modName,
            Dictionary<string, List<string>> idMap,
            Dictionary<string, List<string>> techTypeMap,
            ScanContext context)
        {
            try
            {
                string json = File.ReadAllText(filePath);

                // If this JSON isn't a Nautilus config, deserialization may succeed
                // but Id/TechType will be null and nothing will be registered. That's fine.

                var entries = JsonSerializer.Deserialize<List<NautilusEntry>>(json);
                if (entries != null)
                {
                    foreach (var entry in entries)
                        RegisterEntry(entry, modName, idMap, techTypeMap);
                    return;
                }

                var single = JsonSerializer.Deserialize<NautilusEntry>(json);
                if (single != null)
                {
                    RegisterEntry(single, modName, idMap, techTypeMap);
                    return;
                }
            }
            catch (Exception ex)
            {
                string relative = Path.GetFileName(filePath);
                context.AddNautilusWarning(
                    Severity.Info,
                    $"[{modName}] Failed to parse potential Nautilus config \"{relative}\": {ex.Message}"
                );
            }
        }

        private void RegisterEntry(
            NautilusEntry entry,
            string modName,
            Dictionary<string, List<string>> idMap,
            Dictionary<string, List<string>> techTypeMap)
        {
            if (!string.IsNullOrWhiteSpace(entry.Id))
            {
                if (!idMap.ContainsKey(entry.Id))
                    idMap[entry.Id] = new List<string>();
                if (!idMap[entry.Id].Contains(modName))
                    idMap[entry.Id].Add(modName);
            }

            if (!string.IsNullOrWhiteSpace(entry.TechType))
            {
                if (!techTypeMap.ContainsKey(entry.TechType))
                    techTypeMap[entry.TechType] = new List<string>();
                if (!techTypeMap[entry.TechType].Contains(modName))
                    techTypeMap[entry.TechType].Add(modName);
            }
        }
    }
}
