using System;

namespace ConflictScanner
{
    public class NautilusAnalyzer
    {
        public void Run(ScanContext context)
        {
            context.NautilusWarnings.Add("[TODO] Nautilus analysis not implemented yet.");

            // Later:
            // - Detect duplicate item IDs
            // - Detect duplicate TechTypes
            // - Detect crafting tree conflicts
        }
    }
}
