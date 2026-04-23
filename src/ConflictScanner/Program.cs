using System;
using System.Diagnostics;
using System.IO;

namespace ConflictScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Subnautica Mod Conflict Scanner ===");
            Console.WriteLine();

            string gamePath;

            if (args.Length == 0)
            {
                Console.WriteLine("Enter the path to your Subnautica installation:");
                gamePath = Console.ReadLine()?.Trim() ?? string.Empty;
            }
            else
            {
                gamePath = args[0];
            }

            if (!Directory.Exists(gamePath))
            {
                Console.WriteLine("Error: Directory not found.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Choose scan mode:");
            Console.WriteLine("1. Quick Scan (fast)");
            Console.WriteLine("2. Deep Scan (slower, more thorough)");
            Console.Write("Selection [1/2]: ");

            string choice = Console.ReadLine()?.Trim();
            var mode = choice == "2" ? ScanMode.Deep : ScanMode.Quick;

            var context = new ScanContext(gamePath, mode);

            var stopwatch = Stopwatch.StartNew();

            new HarmonyAnalyzer().Run(context);
            new NautilusAnalyzer().Run(context);
            new QModAnalyzer().Run(context);
            new FileOverrideAnalyzer().Run(context);

            // Suggestion engine will consume the context later
            SuggestionEngine.Generate(context);

            stopwatch.Stop();
            context.ScanDuration = stopwatch.Elapsed;

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
