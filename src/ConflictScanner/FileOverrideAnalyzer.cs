using System;
using System.Collections.Generic;
using System.IO;

namespace ConflictScanner
{
    public class FileOverrideAnalyzer
    {
        public void Run(ScanContext context)
        {
            var fileMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // Scan BepInEx plugins
            string bepPath = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (Directory.Exists(bepPath))
                ScanModFolder(bepPath, fileMap);

            // Scan QMods
            string qmodsPath = Path.Combine(context.GamePath, "QMods");
            if (Directory.Exists(qmodsPath))
                ScanModFolder(qmodsPath, fileMap);

            // Detect conflicts
            foreach (var pair in fileMap)
            {
                if (pair.Value.Count > 1)
                {
                    string mods = string.Join(", ", pair.Value);
                    context.FileWarnings.Add(
                        $"File override conflict: \"{pair.Key}\" is present in multiple mods: {mods}"
                    );
                }
            }
        }

        private void ScanModFolder(string root, Dictionary<string, List<string>> fileMap)
        {
            foreach (var modFolder in Directory.GetDirectories(root))
            {
                string modName = Path.GetFileName(modFolder);

                foreach (var file in Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories))
                {
                    // Normalize path relative to mod root
                    string relative = file.Substring(modFolder.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                          .Replace('\\', '/');

                    // Ignore DLLs — they are handled by Harmony/Nautilus analyzers
                    if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!fileMap.ContainsKey(relative))
                        fileMap[relative] = new List<string>();

                    fileMap[relative].Add(modName);
                }
            }
        }
    }
}
