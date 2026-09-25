using System;
using System.Text;

namespace ConflictScanner
{
    public static class ReportGenerator
    {
        public static string Generate(ScanContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"=== {context.GameName} Conflict Scanner ===");
            sb.AppendLine($"Game path : {context.GamePath}");
            sb.AppendLine($"Mode      : {context.Mode}");
            sb.AppendLine($"Duration  : {context.ScanDuration.TotalSeconds:F1} seconds");
            sb.AppendLine($"Findings  : {context.Findings.Count}");
            sb.AppendLine();

            var categories = new[] { "Harmony", "Nautilus", "SMLHelper", "QMod", "Filesystem" };
            foreach (var cat in categories)
            {
                var categoryFindings = context.Findings.FindAll(f => f.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
                if (categoryFindings.Count == 0)
                    continue;

                sb.AppendLine($"=== {cat} ===");
                foreach (var finding in categoryFindings)
                {
                    sb.AppendLine($"[{finding.Impact} | {finding.Confidence}] {finding.Explanation}");
                }
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
            sb.AppendLine("Some Harmony patches and Nautilus registrations may not be detected if they are created dynamically at runtime.");
            sb.AppendLine("Static and reflection-based analysis focuses on attribute-based and literal declarations; highly dynamic mods may not be fully visible.");
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
