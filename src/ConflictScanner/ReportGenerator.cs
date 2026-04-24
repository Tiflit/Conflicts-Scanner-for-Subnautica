using System;
using System.Text;

namespace ConflictScanner
{
    public static class ReportGenerator
    {
        public static string Generate(ScanContext context)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Subnautica Conflict Scanner ===");
            sb.AppendLine($"Game path : {context.GamePath}");
            sb.AppendLine($"Mode      : {context.Mode}");
            sb.AppendLine($"Duration  : {context.ScanDuration.TotalSeconds:F1} seconds");
            sb.AppendLine();

            AppendSection(sb, "Harmony", context.HarmonyWarnings);
            AppendSection(sb, "Nautilus", context.NautilusWarnings);
            AppendSection(sb, "SMLHelper", context.SMLHelperWarnings);
            AppendSection(sb, "QMod", context.QModWarnings);
            AppendSection(sb, "Files / Overrides", context.FileWarnings);

            AppendPatchers(sb, context);
            AppendSuggestions(sb, context);
            AppendNotes(sb, context);

            return sb.ToString();
        }

        private static void AppendSection(
            StringBuilder sb,
            string title,
            System.Collections.Generic.List<(Severity Level, string Message)> warnings)
        {
            if (warnings.Count == 0)
                return;

            sb.AppendLine($"=== {title} ===");
            foreach (var (level, message) in warnings)
            {
                sb.AppendLine($"[{level}] {message}");
            }
            sb.AppendLine();
        }

        private static void AppendPatchers(StringBuilder sb, ScanContext context)
        {
            sb.AppendLine("=== BepInEx Patchers ===");

            if (context.Patchers.Count == 0)
            {
                sb.AppendLine("No patchers detected.");
            }
            else
            {
                foreach (var p in context.Patchers)
                    sb.AppendLine($"• {p}");
            }

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
            // Always include reflection limitations note.
            sb.AppendLine("=== Notes ===");
            sb.AppendLine("Some Harmony patches and Nautilus registrations may not be detected if they are created dynamically at runtime.");
            sb.AppendLine("Reflection-based analysis focuses on attribute-based and literal-string usage; highly dynamic mods may not be fully visible.");
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
