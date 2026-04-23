using System;
using System.IO;

namespace ConflictScanner
{
    /// <summary>
    /// Detects SMLHelper presence and warns if Nautilus is also present.
    /// </summary>
    public class SMLHelperAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            string bepPlugins = Path.Combine(context.GamePath, "BepInEx", "plugins");
            if (!Directory.Exists(bepPlugins))
            {
                context.AddSMLHelperWarning(
                    Severity.Info,
                    "No BepInEx/plugins folder found. Skipping SMLHelper detection."
                );
                return;
            }

            bool hasSml = false;
            bool hasNautilus = false;

            foreach (var dll in Directory.GetFiles(bepPlugins, "*.dll", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(dll);

                if (fileName.Equals("SMLHelper.dll", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Contains("SMLHelper", StringComparison.OrdinalIgnoreCase))
                {
                    hasSml = true;
                }

                if (fileName.Equals("Nautilus.dll", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Contains("Nautilus", StringComparison.OrdinalIgnoreCase))
                {
                    hasNautilus = true;
                }
            }

            if (!hasSml && !hasNautilus)
            {
                context.AddSMLHelperWarning(
                    Severity.Info,
                    "No SMLHelper or Nautilus assemblies detected."
                );
                return;
            }

            if (hasSml)
            {
                context.AddSMLHelperWarning(
                    Severity.Info,
                    "SMLHelper detected in BepInEx/plugins."
                );
            }

            if (hasNautilus)
            {
                context.AddNautilusWarning(
                    Severity.Info,
                    "Nautilus detected in BepInEx/plugins."
                );
            }

            if (hasSml && hasNautilus)
            {
                context.AddSMLHelperWarning(
                    Severity.Error,
                    "Both SMLHelper and Nautilus detected. These are mutually incompatible; use one or the other."
                );
            }
        }
    }
}
