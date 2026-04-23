using System;

namespace ConflictScanner.Reflection
{
    /// <summary>
    /// Placeholder for future reflection-based Nautilus analysis.
    /// This will detect TechType registrations, CraftTree nodes,
    /// sprite registrations, and prefab handlers directly from assemblies.
    /// </summary>
    public class NautilusReflectionAnalyzer
    {
        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
                return;

            // Deep Scan placeholder
            context.AddNautilusWarning(
                Severity.Info,
                "Deep Scan: Nautilus reflection analysis not implemented yet."
            );
        }
    }
}
