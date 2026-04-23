using System;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Placeholder for future reflection-based Harmony patch analysis.
    /// This will load assemblies in a safe AssemblyLoadContext and inspect
    /// Harmony patch metadata (prefixes, postfixes, transpilers, priorities).
    /// </summary>
    public class HarmonyReflectionAnalyzer
    {
        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
                return;

            // Deep Scan placeholder
            context.AddHarmonyWarning(
                Severity.Info,
                "Deep Scan: Harmony reflection analysis not implemented yet."
            );
        }
    }
}
