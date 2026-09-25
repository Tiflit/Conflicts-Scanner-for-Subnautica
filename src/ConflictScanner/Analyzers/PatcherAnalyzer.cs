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
                context.FileWarnings.Add((
                    Severity.Info,
                    "No BepInEx folder found. Skipping patcher analysis."
                ));
                return;
            }

            if (!Directory.Exists(patchersPath))
            {
                return;
            }

            var patcherDlls = new List<string>();

            foreach (var dll in Directory.GetFiles(patchersPath, "*.dll", SearchOption.AllDirectories))
            {
                string relative = dll.Substring(patchersPath.Length)
                                     .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                     .Replace('\\', '/');
                patcherDlls.Add(relative);
                context.AddPatcher(relative);

                context.AddFinding(new Finding
                {
                    Category = "Patcher",
                    Impact = Impact.Info,
                    Confidence = Confidence.Observed,
                    ResourceKey = relative,
                    Evidence = $"Preloader patcher DLL: BepInEx/patchers/{relative}",
                    Explanation = $"Preloader patcher \"{relative}\" detected. Patchers execute before the game initializes and modify game assemblies directly in memory.",
                    SuggestedAction = "Informational: Verify that this patcher is intended and up to date."
                });
            }

            if (patcherDlls.Count == 0)
                return;

            context.FileWarnings.Add((
                Severity.Warning,
                $"Detected {patcherDlls.Count} patcher(s) in BepInEx/patchers."
            ));
        }
    }
}
