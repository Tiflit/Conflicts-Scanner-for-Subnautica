using System.Text;
using System.Collections.Generic;

namespace ConflictScanner
{
    public static class ReportGenerator
    {
        public static string Generate(ScanContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Subnautica Mod Conflict Report ===");
            sb.AppendLine();
            sb.AppendLine($"Scan mode: {context.Mode}");
            sb.AppendLine($"Duration: {context.ScanDuration.TotalSeconds:F2} seconds");
            sb.AppendLine();

            AppendSection(sb, "Harmony Conflicts", context.HarmonyWarnings);
            AppendSection(sb, "Nautilus Conflicts", context.NautilusWarnings);
            AppendSection(sb, "QMod Conflicts", context.QModWarnings);
            AppendSection(sb, "File Conflicts", context.FileWarnings);

            AppendSuggestions(sb, context.Suggestions);

            return sb.ToString();
        }

        private static void AppendSection(
            StringBuilder sb,
            string title,
            List<(Severity Level, string Message)> items)
        {
            sb.AppendLine($"## {title}");

            if (items.Count == 0)
            {
                sb.AppendLine("No issues detected.");
            }
            else
            {
                foreach (var (level, message) in items)
                {
                    sb.AppendLine($"[{level}] {message}");
                }
            }

            sb.AppendLine();
        }

        private static void AppendSuggestions(StringBuilder sb, List<string> suggestions)
        {
            sb.AppendLine("## Suggestions");

            if (suggestions.Count == 0)
            {
                sb.AppendLine("No suggestions available yet.");
            }
            else
            {
                foreach (var suggestion in suggestions)
                {
                    sb.AppendLine($"- {suggestion}");
                }
            }

            sb.AppendLine();
        }
    }
}
