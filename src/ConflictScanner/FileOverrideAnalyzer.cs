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

            string bepPath = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepPath))
                ScanModFolder(bepPath, pathMap, hashMap, context);

            string qmodsPath = Path.Combine(context.GamePath, "QMods");
            if (Directory.Exists(qmodsPath))
                ScanModFolder(qmodsPath, pathMap, hashMap, context);

            // Path conflicts
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

            // Content conflicts
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
            Dictionary<string, List<string>> hashMap,
            ScanContext context)
        {
            foreach (var modFolder in Directory.GetDirectories(root))
            {
                string modName = Path.GetFileName(modFolder);

                foreach (var file in Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(modFolder.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          .Replace('\\', '/');

                    if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Heuristic checks
                    RunHeuristics(file, modName, relative, context);

                    // Path conflict tracking
                    if (!pathMap.ContainsKey(relative))
                        pathMap[relative] = new List<string>();
                    pathMap[relative].Add(modName);

                    // Hash conflict tracking
                    string hash = ComputeHash(file);
                    if (!hashMap.ContainsKey(hash))
                        hashMap[hash] = new List<string>();
                    hashMap[hash].Add($"{modName}:{relative}");
                }
            }
        }

        private void RunHeuristics(string filePath, string modName, string relative, ScanContext context)
        {
            FileInfo info = new FileInfo(filePath);

            // 1. Zero-byte files
            if (info.Length == 0)
            {
                context.FileWarnings.Add(
                    $"[{modName}] Zero-byte file: \"{relative}\""
                );
            }

            // 2. Very large files (> 50 MB)
            if (info.Length > 50 * 1024 * 1024)
            {
                context.FileWarnings.Add(
                    $"[{modName}] Large file (>50MB): \"{relative}\" ({info.Length / (1024 * 1024)} MB)"
                );
            }

            // 3. Suspicious file types
            string ext = Path.GetExtension(relative).ToLowerInvariant();
            if (ext == ".meta" || ext == ".manifest" || ext == ".tmp" || ext == ".bak")
            {
                context.FileWarnings.Add(
                    $"[{modName}] Suspicious file type: \"{relative}\""
                );
            }

            // 4. File type mismatch heuristics
            if (ext == ".png" && !LooksLikePng(filePath))
            {
                context.FileWarnings.Add(
                    $"[{modName}] PNG file does not appear to be a valid PNG: \"{relative}\""
                );
            }

            if (ext == ".json" && !LooksLikeJson(filePath))
            {
                context.FileWarnings.Add(
                    $"[{modName}] JSON file may be invalid: \"{relative}\""
                );
            }
        }

        private bool LooksLikePng(string file)
        {
            try
            {
                byte[] header = new byte[8];
                using var stream = File.OpenRead(file);
                stream.Read(header, 0, 8);

                // PNG signature: 89 50 4E 47 0D 0A 1A 0A
                return header[0] == 0x89 &&
                       header[1] == 0x50 &&
                       header[2] == 0x4E &&
                       header[3] == 0x47;
            }
            catch { return false; }
        }

        private bool LooksLikeJson(string file)
        {
            try
            {
                string text = File.ReadAllText(file).Trim();
                return text.StartsWith("{") || text.StartsWith("[");
            }
            catch { return false; }
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
