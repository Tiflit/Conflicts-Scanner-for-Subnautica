using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConflictScanner
{
    public class FileOverrideAnalyzer : IAnalyzer
    {
        private const long MaxHashSize = 100 * 1024 * 1024; // 100 MB

        public void Run(ScanContext context)
        {
            var assemblyMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var hashMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var modFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string bepPlugins = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepPlugins))
                ScanModFolder(bepPlugins, assemblyMap, hashMap, modFolderNames, context);

            string qmodsPath = Path.Combine(context.GamePath, "QMods");
            if (Directory.Exists(qmodsPath))
                ScanModFolder(qmodsPath, assemblyMap, hashMap, modFolderNames, context);

            // Check for loose root plugins vs mod folders
            CheckLoosePlugins(bepPlugins, modFolderNames, context);

            // Check for shared assembly filename collisions across different mod folders
            foreach (var (dllName, mods) in assemblyMap)
            {
                if (mods.Count > 1)
                {
                    string msg = $"Assembly collision: \"{dllName}\" is bundled by multiple mods ({string.Join(", ", mods)}). The game loader may load an unexpected version.";
                    context.FileWarnings.Add((Severity.Warning, msg));
                    context.AddFinding(new Finding
                    {
                        Category = "Filesystem",
                        Impact = Impact.High,
                        Confidence = Confidence.Observed,
                        InvolvedMods = mods,
                        ResourceKey = dllName,
                        Evidence = $"Assembly \"{dllName}\" bundled by mods: {string.Join(", ", mods)}",
                        Explanation = msg,
                        SuggestedAction = "Ensure both mods share a compatible version of this library or move shared libraries to BepInEx/plugins root."
                    });
                }
            }

            // Check for duplicate identical content (Informational in Deep mode)
            if (context.Mode == ScanMode.Deep)
            {
                foreach (var pair in hashMap)
                {
                    if (pair.Value.Count > 1)
                    {
                        string msg = $"Identical content shared (hash {pair.Key.Substring(0, 12)}…): {string.Join(", ", pair.Value)}";
                        context.FileWarnings.Add((Severity.Info, msg));
                        context.AddFinding(new Finding
                        {
                            Category = "Filesystem",
                            Impact = Impact.Info,
                            Confidence = Confidence.Observed,
                            ResourceKey = pair.Key,
                            Evidence = $"SHA-256 hash match: {pair.Key.Substring(0, 12)}…",
                            Explanation = msg,
                            SuggestedAction = "Informational: These files contain identical binary content."
                        });
                    }
                }
            }
        }

        private void ScanModFolder(
            string root,
            Dictionary<string, List<string>> assemblyMap,
            Dictionary<string, List<string>> hashMap,
            HashSet<string> modFolderNames,
            ScanContext context)
        {
            foreach (var modFolder in Directory.GetDirectories(root))
            {
                string modName = Path.GetFileName(modFolder);

                if (!modFolderNames.Add(modName))
                {
                    string msg = $"Duplicate mod folder detected: \"{modName}\" exists in multiple locations.";
                    context.FileWarnings.Add((Severity.Error, msg));
                    context.AddFinding(new Finding
                    {
                        Category = "Filesystem",
                        Impact = Impact.Critical,
                        Confidence = Confidence.Observed,
                        InvolvedMods = new[] { modName },
                        ResourceKey = modName,
                        Evidence = $"Mod folder \"{modName}\" exists in multiple directories",
                        Explanation = msg,
                        SuggestedAction = "Remove the duplicate copy of this mod folder."
                    });
                }

                foreach (var file in Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);
                    string relative = file.Substring(modFolder.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          .Replace('\\', '/');

                    if (IgnoreList.ShouldIgnore(relative))
                        continue;

                    // Track bundled DLL assemblies across mods
                    if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!assemblyMap.ContainsKey(fileName))
                            assemblyMap[fileName] = new List<string>();

                        if (!assemblyMap[fileName].Contains(modName))
                            assemblyMap[fileName].Add(modName);

                        continue;
                    }

                    if (context.Mode == ScanMode.Quick)
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            if (info.Length == 0)
                            {
                                string msg = $"[{modName}] Zero-byte file: \"{relative}\"";
                                context.FileWarnings.Add((Severity.Warning, msg));
                                context.AddFinding(new Finding
                                {
                                    Category = "Filesystem",
                                    Impact = Impact.Low,
                                    Confidence = Confidence.Observed,
                                    InvolvedMods = new[] { modName },
                                    ResourceKey = relative,
                                    Evidence = $"File size is 0 bytes: \"{relative}\"",
                                    Explanation = msg,
                                    SuggestedAction = "Remove or re-download the empty file."
                                });
                            }
                        }
                        catch
                        {
                            // Skip inaccessible file in quick mode
                        }
                        continue;
                    }

                    RunHeuristics(file, modName, relative, context);

                    try
                    {
                        var deepInfo = new FileInfo(file);
                        if (deepInfo.Length <= MaxHashSize)
                        {
                            string hash = ComputeHash(file);
                            if (!string.IsNullOrEmpty(hash))
                            {
                                if (!hashMap.ContainsKey(hash))
                                    hashMap[hash] = new List<string>();
                                hashMap[hash].Add($"{modName}:{relative}");
                            }
                        }
                        else
                        {
                            context.FileWarnings.Add((
                                Severity.Info,
                                $"[{modName}] Skipped hashing large file (>100MB): \"{relative}\""
                            ));
                        }
                    }
                    catch
                    {
                        // Non-critical hash failure
                    }
                }
            }
        }

        private void CheckLoosePlugins(string pluginsPath, HashSet<string> modFolderNames, ScanContext context)
        {
            if (!Directory.Exists(pluginsPath))
                return;

            foreach (var looseFile in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(looseFile);
                if (modFolderNames.Contains(nameWithoutExt))
                {
                    string msg = $"Loose plugin \"{Path.GetFileName(looseFile)}\" in BepInEx/plugins may shadow or conflict with folder \"{nameWithoutExt}\".";
                    context.FileWarnings.Add((Severity.Warning, msg));
                    context.AddFinding(new Finding
                    {
                        Category = "Filesystem",
                        Impact = Impact.Medium,
                        Confidence = Confidence.Observed,
                        InvolvedMods = new[] { nameWithoutExt },
                        ResourceKey = Path.GetFileName(looseFile),
                        Evidence = $"Loose DLL \"{Path.GetFileName(looseFile)}\" matches folder \"{nameWithoutExt}\"",
                        Explanation = msg,
                        SuggestedAction = "Check whether this is an orphaned DLL from a previous mod version and remove it if obsolete."
                    });
                }
            }
        }

        private void RunHeuristics(string filePath, string modName, string relative, ScanContext context)
        {
            FileInfo info;
            try
            {
                info = new FileInfo(filePath);
            }
            catch
            {
                return;
            }

            string ext = Path.GetExtension(relative).ToLowerInvariant();

            if (info.Length == 0)
            {
                string msg = $"[{modName}] Zero-byte file: \"{relative}\"";
                context.FileWarnings.Add((Severity.Warning, msg));
                context.AddFinding(new Finding
                {
                    Category = "Filesystem",
                    Impact = Impact.Low,
                    Confidence = Confidence.Observed,
                    InvolvedMods = new[] { modName },
                    ResourceKey = relative,
                    Evidence = $"File size is 0 bytes: \"{relative}\"",
                    Explanation = msg,
                    SuggestedAction = "Remove or re-download the empty file."
                });
                return;
            }

            if (info.Length > 50 * 1024 * 1024)
            {
                string msg = $"[{modName}] Large file (>50MB): \"{relative}\" ({info.Length / (1024 * 1024)} MB)";
                context.FileWarnings.Add((Severity.Warning, msg));
                context.AddFinding(new Finding
                {
                    Category = "Filesystem",
                    Impact = Impact.Low,
                    Confidence = Confidence.Observed,
                    InvolvedMods = new[] { modName },
                    ResourceKey = relative,
                    Evidence = $"File size is {info.Length / (1024 * 1024)} MB",
                    Explanation = msg,
                    SuggestedAction = "Informational: Large assets may increase game load times."
                });
            }

            if (ext == ".meta" || ext == ".manifest" || ext == ".tmp" || ext == ".bak")
            {
                string msg = $"[{modName}] Leftover or temporary file: \"{relative}\"";
                context.FileWarnings.Add((Severity.Info, msg));
                context.AddFinding(new Finding
                {
                    Category = "Filesystem",
                    Impact = Impact.Info,
                    Confidence = Confidence.Observed,
                    InvolvedMods = new[] { modName },
                    ResourceKey = relative,
                    Evidence = $"File has temporary/leftover extension: {ext}",
                    Explanation = msg,
                    SuggestedAction = "Safe to delete if not needed by mod development tooling."
                });
            }

            if (ext == ".png")
            {
                if (!LooksLikePng(filePath))
                {
                    string msg = $"[{modName}] PNG file appears corrupted or invalid header: \"{relative}\"";
                    context.FileWarnings.Add((Severity.Critical, msg));
                    context.AddFinding(new Finding
                    {
                        Category = "Filesystem",
                        Impact = Impact.Critical,
                        Confidence = Confidence.Observed,
                        InvolvedMods = new[] { modName },
                        ResourceKey = relative,
                        Evidence = $"Corrupt PNG magic header in \"{relative}\"",
                        Explanation = msg,
                        SuggestedAction = "Replace or re-download the corrupted image asset."
                    });
                }
                return;
            }

            if (ext == ".json")
            {
                if (!LooksLikeJson(filePath))
                {
                    string msg = $"[{modName}] JSON file may be malformed (does not start with '{{' or '['): \"{relative}\"";
                    context.FileWarnings.Add((Severity.Warning, msg));
                    context.AddFinding(new Finding
                    {
                        Category = "Filesystem",
                        Impact = Impact.High,
                        Confidence = Confidence.Observed,
                        InvolvedMods = new[] { modName },
                        ResourceKey = relative,
                        Evidence = $"Malformed JSON file: \"{relative}\"",
                        Explanation = msg,
                        SuggestedAction = "Validate JSON syntax in this configuration file."
                    });
                }
                return;
            }

            string mime = MimeDetector.DetectMime(filePath);
            if (ext == ".ogg" && mime != "audio/ogg")
            {
                string msg = $"[{modName}] File extension mismatch: \"{relative}\" is OGG but detected as {mime}";
                context.FileWarnings.Add((Severity.Error, msg));
                context.AddFinding(new Finding
                {
                    Category = "Filesystem",
                    Impact = Impact.High,
                    Confidence = Confidence.Observed,
                    InvolvedMods = new[] { modName },
                    ResourceKey = relative,
                    Evidence = $"File \"{relative}\" has extension .ogg but MIME is {mime}",
                    Explanation = msg,
                    SuggestedAction = "Ensure audio files are properly encoded as OGG Vorbis."
                });
            }
            else if (ext == ".txt" && mime == "application/octet-stream")
            {
                string msg = $"[{modName}] Text file appears to be binary: \"{relative}\"";
                context.FileWarnings.Add((Severity.Warning, msg));
                context.AddFinding(new Finding
                {
                    Category = "Filesystem",
                    Impact = Impact.Low,
                    Confidence = Confidence.Observed,
                    InvolvedMods = new[] { modName },
                    ResourceKey = relative,
                    Evidence = $"File \"{relative}\" has extension .txt but MIME is binary",
                    Explanation = msg,
                    SuggestedAction = "Informational: File has .txt extension but contains non-text binary data."
                });
            }
        }

        private bool LooksLikePng(string file)
        {
            try
            {
                byte[] header = new byte[8];
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int read = stream.Read(header, 0, 8);
                if (read < 8) return false;

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
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 512);

                int ch;
                while ((ch = reader.Read()) != -1)
                {
                    char c = (char)ch;
                    if (!char.IsWhiteSpace(c))
                        return c == '{' || c == '[';
                }

                return false;
            }
            catch { return false; }
        }

        private string ComputeHash(string filePath)
        {
            try
            {
                using var sha = SHA256.Create();
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] hashBytes = sha.ComputeHash(stream);

                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
