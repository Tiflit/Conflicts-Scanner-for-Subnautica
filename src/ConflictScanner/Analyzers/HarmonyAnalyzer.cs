using System;

namespace ConflictScanner
{
    public class HarmonyAnalyzer
    {
        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
            {
                // Quick Scan: very light checks only
                context.AddHarmonyWarning(
                    Severity.Info,
                    "Quick Scan: Harmony analysis skipped (Deep Scan recommended for patch conflict detection)."
                );
                return;
            }

            // Deep Scan: placeholder for future reflection-based analysis
            context.AddHarmonyWarning(
                Severity.Info,
                "Deep Scan: Harmony patch analysis not implemented yet."
            );
        }
    }
}
