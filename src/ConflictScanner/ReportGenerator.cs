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

            AppendSection(sb, "Harmony Conflicts", context.HarmonyWarnings);
            AppendSection(sb, "Nautilus Conflicts", context.NautilusWarnings);
            AppendSection(sb, "QMod Conflicts", context.QModWarnings);
            AppendSection(sb, "File Conflicts", context.FileWarnings);

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
    }
}
