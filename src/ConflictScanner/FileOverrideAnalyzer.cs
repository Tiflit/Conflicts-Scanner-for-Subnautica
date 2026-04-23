using System;

namespace ConflictScanner
{
    public class FileOverrideAnalyzer
    {
        public void Run(ScanContext context)
        {
            context.FileWarnings.Add("[TODO] File override analysis not implemented yet.");

            // Later:
            // - Hash files
            // - Detect multiple mods overriding the same file
        }
    }
}
