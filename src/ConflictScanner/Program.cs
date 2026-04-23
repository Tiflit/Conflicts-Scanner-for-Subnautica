using System;
using System.Diagnostics;
using System.IO;
using ConflictScanner.Profiles;

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

            // Detect game profile
            var profile = ProfileManager.DetectProfile(gamePath);

            if (profile == null)
            {
                Console.WriteLine("Could not detect a supported game at this path.");
                return;
            }

            Console.WriteLine($"Detected game: {profile.GameName}");
            Console.WriteLine();

            // Choose scan mode
            Console.WriteLine("Choose scan mode:");
            Console.WriteLine("1. Quick Scan (fast)");
            Console.WriteLine("2. Deep Scan (slower, more thorough)");
            Console.Write("Selection [1/2]: ");

            string choice = Console.ReadLine()?.Trim();
            var mode = choice == "2" ? ScanMode.Deep : ScanMode.Quick;

            var context = new ScanContext(gamePath, mode);

            // Build analyzer pipeline
            var pipeline = new AnalyzerPipeline();
            profile.RegisterAnalyzers(pipeline);

            var stopwatch = Stopwatch.StartNew();

            // Run analyzers in order
            foreach (var analyzer in pipeline.GetAnalyzers())
            {
                switch (analyzer)
                {
                    case HarmonyAnalyzer h: h.Run(context); break;
                    case NautilusAnalyzer n: n.Run(context); break;
                    case QModAnalyzer q: q.Run(context); break;
                    case FileOverrideAnalyzer f: f.Run(context); break;
                }
            }

            // Suggestions
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
