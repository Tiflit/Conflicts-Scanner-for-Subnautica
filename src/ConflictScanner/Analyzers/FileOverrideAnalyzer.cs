using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConflictScanner
{
    public class FileOverrideAnalyzer
    {
        private const long MaxHashSize = 100 * 1024 * 1024; // 100 MB

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

            foreach (var pair in pathMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.AddFileWarning(
                        Severity.Error,
                        $"Path conflict: \"{pair.Key}\" appears in multiple mods: {mods}"
                    );
                }
            }

            if (context.Mode == ScanMode.Deep)
            {
                foreach (var pair in hashMap)
                {
                    if (pair.Value.Count > 1)
                    {
                        string mods = string.Join(", ", pair.Value);
                        context.AddFileWarning(
                            Severity.Warning,
                            $"Duplicate content detected (hash {pair.Key.Substring(0, 12)}…): used by mods: {mods}"
                        );
                    }
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

                    if (IgnoreList.ShouldIgnore(relative))
                        continue;

                    RunHeuristics(file, modName, relative, context);

                    if (!pathMap.ContainsKey(relative))
                        pathMap[relative] = new List<string>();
                    pathMap[relative].Add(modName);

                    if (context.Mode == ScanMode.Deep)
                    {
                        FileInfo info = new FileInfo(file);

                        if (info.Length <= MaxHashSize)
                        {
                            string hash = ComputeHash(file);
                            if (!hashMap.ContainsKey(hash))
                                hashMap[hash] = new List<string>();
                            hashMap[hash].Add($"{modName}:{relative}");
                        }
                        else
                        {
                            context.AddFileWarning(
                                Severity.Info,
                                $"[{modName}] Skipped hashing large file (>100MB): \"{relative}\""
                            );
                        }
                    }
                }
            }
        }

        private void RunHeuristics(string filePath, string modName, string relative, ScanContext context)
        {
            FileInfo info = new FileInfo(filePath);
            string ext = Path.GetExtension(relative).ToLowerInvariant();

            if (info.Length == 0)
            {
                context.AddFileWarning(Severity.Warning, $"[{modName}] Zero-byte file: \"{relative}\"");
            }

            if (info.Length > 50 * 1024 * 1024)
            {
                context.AddFileWarning(
                    Severity.Warning,
                    $"[{modName}] Large file (>50MB): \"{relative}\" ({info.Length / (1024 * 1024)} MB)"
                );
            }

            if (ext == ".meta" || ext == ".manifest" || ext == ".tmp" || ext == ".bak")
            {
                context.AddFileWarning(
                    Severity.Info,
                    $"[{modName}] Suspicious file type: \"{relative}\""
                );
            }

            bool looksJson = ext == ".json" && LooksLikeJson(filePath);

            if (ext == ".png" && !LooksLikePng(filePath))
            {
                context.AddFileWarning(
                    Severity.Critical,
                    $"[{modName}] PNG file does not appear to be valid: \"{relative}\""
                );
            }

            if (ext == ".json" && !looksJson)
            {
                context.AddFileWarning(
                    Severity.Warning,
                    $"[{modName}] JSON file may be invalid: \"{relative}\""
                );
            }

            // MIME checks only if JSON looks structurally like JSON
            string mime = MimeDetector.DetectMime(filePath);

            if (ext == ".png" && mime != "image/png")
            {
                context.AddFileWarning(
                    Severity.Error,
                    $"[{modName}] File extension mismatch: \"{relative}\" is PNG but detected as {mime}"
                );
            }

            if (ext == ".ogg" && mime != "audio/ogg")
            {
                context.AddFileWarning(
                    Severity.Error,
                    $"[{modName}] File extension mismatch: \"{relative}\" is OGG but detected as {mime}"
                );
            }

            if (ext == ".json" && looksJson &&
                mime != "application/json" && mime != "text/plain")
            {
                context.AddFileWarning(
                    Severity.Error,
                    $"[{modName}] JSON file appears invalid or binary: \"{relative}\" (detected {mime})"
                );
            }

            if ((ext == ".txt" || ext == ".json") &&
                mime == "application/octet-stream")
            {
                context.AddFileWarning(
                    Severity.Warning,
                    $"[{modName}] Text file appears to be binary: \"{relative}\""
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
