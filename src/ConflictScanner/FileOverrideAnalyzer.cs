using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConflictScanner
{
    public class FileOverrideAnalyzer
    {
        public void Run(ScanContext context)
        {
            var pathMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var hashMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // Scan BepInEx plugins
            string bepPath = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepPath))
                ScanModFolder(bepPath, pathMap, hashMap);

            // Scan QMods
            string qmodsPath = Path.Combine(context.GamePath, "QMods");
            if (Directory.Exists(qmodsPath))
                ScanModFolder(qmodsPath, pathMap, hashMap);

            // Detect path conflicts (same relative path)
            foreach (var pair in pathMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.FileWarnings.Add(
                        $"Path conflict: \"{pair.Key}\" appears in multiple mods: {mods}"
                    );
                }
            }

            // Detect content conflicts (same hash)
            foreach (var pair in hashMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.FileWarnings.Add(
                        $"Duplicate content detected (hash {pair.Key.Substring(0, 12)}…): used by mods: {mods}"
                    );
                }
            }
        }

        private void ScanModFolder(
            string root,
            Dictionary<string, List<string>> pathMap,
            Dictionary<string, List<string>> hashMap)
        {
            foreach (var modFolder in Directory.GetDirectories(root))
            {
                string modName = Path.GetFileName(modFolder);

                foreach (var file in Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories))
                {
                    // Normalize relative path
                    string relative = file.Substring(modFolder.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          .Replace('\\', '/');

                    // Skip DLLs — handled by Harmony/Nautilus analyzers
                    if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Register path conflict
                    if (!pathMap.ContainsKey(relative))
                        pathMap[relative] = new List<string>();
                    pathMap[relative].Add(modName);

                    // Compute hash
                    string hash = ComputeHash(file);

                    if (!hashMap.ContainsKey(hash))
                        hashMap[hash] = new List<string>();
                    hashMap[hash].Add($"{modName}:{relative}");
                }
            }
        }

        private string ComputeHash(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = sha.ComputeHash(stream);

            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
