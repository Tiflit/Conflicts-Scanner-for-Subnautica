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
                Console.WriteLine("Enter the path to your Subnautica installation:");
                string input = Console.ReadLine();
                args = new[] { input };
            }

            string gamePath = args[0];

            if (!Directory.Exists(gamePath))
            {
                Console.WriteLine("Error: Directory not found.");
                return;
            }

            var context = new ScanContext(gamePath);

            new HarmonyAnalyzer().Run(context);
            new NautilusAnalyzer().Run(context);
            new QModAnalyzer().Run(context);
            new FileOverrideAnalyzer().Run(context);

            string report = ReportGenerator.Generate(context);

            Console.WriteLine();
            Console.WriteLine("=== Scan Complete ===");
            Console.WriteLine(report);

            Console.WriteLine("Save report to file? (y/n)");
            string save = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (save == "y")
            {
                File.WriteAllText("ConflictReport.txt", report);
                Console.WriteLine("Report saved as ConflictReport.txt");
            }
        }
    }
}
