using System;
using System.IO;

namespace ConflictScanner
{
    public class QModAnalyzer
    {
        public void Run(ScanContext context)
        {
            string qmodsPath = Path.Combine(context.GamePath, "QMods");

            if (!Directory.Exists(qmodsPath))
            {
                context.QModWarnings.Add("No QMods folder found.");
                return;
            }

            context.QModWarnings.Add("[TODO] QMod analysis not implemented yet.");

            // Later:
            // - Parse mod.json
            // - Detect duplicate IDs
            // - Detect missing dependencies
        }
    }
}
