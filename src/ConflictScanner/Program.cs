using System;
using System.IO;

namespace ConflictScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Subnautica Mod Conflict Scanner ===");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ConflictScanner <path-to-Subnautica-folder>");
                return;
            }

            string gamePath = args[0];

            if (!Directory.Exists(gamePath))
            {
                Console.WriteLine("Error: Directory not found.");
                return;
            }

            var context = new ScanContext(gamePath);

            // Run analyzers
            new HarmonyAnalyzer().Run(context);
            new NautilusAnalyzer().Run(context);
            new QModAnalyzer().Run(context);
            new FileOverrideAnalyzer().Run(context);

            // Generate report
            string report = ReportGenerator.Generate(context);
            File.WriteAllText("ConflictReport.txt", report);

            Console.WriteLine("Scan complete. Report saved as ConflictReport.txt");
        }
    }
}
