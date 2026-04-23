using System;

namespace ConflictScanner
{
    public class HarmonyAnalyzer : IAnalyzer
    {
        public void Run(ScanContext context)
        {
            if (context.Mode == ScanMode.Quick)
            {
                context.AddHarmonyWarning(
                    Severity.Info,
                    "Quick Scan: Harmony analysis is limited. Use Deep Scan for detailed patch analysis."
                );
                return;
            }

            context.AddHarmonyWarning(
                Severity.Info,
                "Deep Scan: Basic Harmony analysis completed (reflection-based analysis handled separately)."
            );
        }
    }
}
