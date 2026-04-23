using System;

namespace ConflictScanner
{
    public class HarmonyAnalyzer
    {
        public void Run(ScanContext context)
        {
            // Quick vs Deep behavior can be added later
            context.AddHarmonyWarning(
                Severity.Info,
                "[TODO] Harmony analysis not implemented yet."
            );
        }
    }
}
