using System;
using System.Linq;
using System.Text;
using ConflictScanner.Profiles;

namespace ConflictScanner
{
    public static class ReportGenerator
    {
        public static string Generate(ScanContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"=== {context.GameName} Conflict Scanner ===");
            sb.AppendLine($"Game path : {context.GamePath}");
            if (context.Environment != null && context.Environment.Branch != GameBranch.Unknown)
            {
                sb.AppendLine($"Branch    : {context.Environment.Branch} (Version: {context.Environment.VersionString})");
            }
            sb.AppendLine($"Mode      : {context.Mode}");
            sb.AppendLine($"Duration  : {context.ScanDuration.TotalSeconds:F1} seconds");
            sb.AppendLine($"Findings  : {context.Findings.Count}");
            sb.AppendLine();

            var categories = context.Findings
                .Select(f => f.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c);

            foreach (var cat in categories)
            {
                var categoryFindings = context.Findings.FindAll(f => f.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
                if (categoryFindings.Count == 0)
                    continue;

                sb.AppendLine($"=== {cat} ===");
                foreach (var finding in categoryFindings)
                {
                    string mods = finding.InvolvedMods.Count > 0 ? $" [{string.Join(", ", finding.InvolvedMods)}]" : string.Empty;
                    sb.AppendLine($"[{finding.Impact} | {finding.Confidence}]{mods} {finding.Explanation}");
                    if (!string.IsNullOrWhiteSpace(finding.SuggestedAction))
                    {
                        sb.AppendLine($"   → Action: {finding.SuggestedAction}");
                    }
                }
                sb.AppendLine();
            }

            if (context.Findings.Count == 0)
            {
                sb.AppendLine("No conflicts or compatibility issues detected.");
                sb.AppendLine();
            }

            AppendPatchers(sb, context);
            AppendSuggestions(sb, context);
            AppendNotes(sb, context);

            return sb.ToString();
        }

        private static void AppendPatchers(StringBuilder sb, ScanContext context)
        {
            if (context.Patchers.Count == 0)
                return;

            sb.AppendLine("=== BepInEx Patchers ===");
            foreach (var p in context.Patchers)
                sb.AppendLine($"• {p}");
            sb.AppendLine();
        }

        private static void AppendSuggestions(StringBuilder sb, ScanContext context)
        {
            if (context.Suggestions.Count == 0)
                return;

            sb.AppendLine("=== Suggestions ===");
            foreach (var s in context.Suggestions)
                sb.AppendLine($"• {s}");
            sb.AppendLine();
        }

        private static void AppendNotes(StringBuilder sb, ScanContext context)
        {
            sb.AppendLine("=== Notes ===");
            sb.AppendLine("Static analysis inspects attributes, dependencies, and bytecode without loading untrusted assemblies into the scanner process.");
            sb.AppendLine("Dynamic registrations executed entirely at runtime may not be fully visible statically.");
            sb.AppendLine();

            if (context.Notes.Count > 0)
            {
                foreach (var note in context.Notes)
                    sb.AppendLine($"• {note}");
                sb.AppendLine();
            }
        }
    }
}
