using System;

namespace ConflictScanner
{
    public class HarmonyAnalyzer
    {
        public void Run(ScanContext context)
        {
            context.HarmonyWarnings.Add("[TODO] Harmony analysis not implemented yet.");

            // Later:
            // - Load DLLs in isolated AssemblyLoadContext
            // - Detect Harmony patches
            // - Build patch map
            // - Identify conflicts
        }
    }
}
