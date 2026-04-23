using System;
using System.Collections.Generic;
using System.IO;

namespace ConflictScanner
{
    /// <summary>
    /// Scans BepInEx/patchers for IL patchers and reports their presence.
    /// Patchers run before the game loads and can be highly impactful.
    /// </summary>
    public class PatcherAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            string bepRoot = Path.Combine(context.GamePath, "BepInEx");
            string patchersPath = Path.Combine(bepRoot, "patchers");

            if (!Directory.Exists(bepRoot))
            {
                context.AddFileWarning(
                    Severity.Info,
                    "No BepInEx folder found. Skipping patcher analysis."
                );
                return;
            }

            if (!Directory.Exists(patchersPath))
            {
                context.AddFileWarning(
                    Severity.Info,
                    "No BepInEx/patchers folder found."
                );
                return;
            }

            var patcherDlls = new List<string>();

            foreach (var dll in Directory.GetFiles(patchersPath, "*.dll", SearchOption.AllDirectories))
            {
                string relative = dll.Substring(patchersPath.Length)
                                     .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                     .Replace('\\', '/');
                patcherDlls.Add(relative);
            }

            if (patcherDlls.Count == 0)
            {
                context.AddFileWarning(
                    Severity.Info,
                    "BepInEx/patchers exists but contains no DLLs."
                );
                return;
            }

            context.AddFileWarning(
                Severity.Warning,
                $"Detected {patcherDlls.Count} patcher(s) in BepInEx/patchers: {string.Join(", ", patcherDlls)}"
            );
        }
    }
}
