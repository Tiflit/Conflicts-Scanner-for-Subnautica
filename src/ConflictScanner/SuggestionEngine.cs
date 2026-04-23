namespace ConflictScanner
{
    public static class SuggestionEngine
    {
        public static void Generate(ScanContext context)
        {
            // Stage 1: simple, generic suggestions based on existing findings.
            // We'll flesh this out later.

            if (context.FileWarnings.Count > 0)
            {
                context.Suggestions.Add(
                    "You have file conflicts or anomalies. Consider reviewing mods that appear repeatedly in File Conflicts."
                );
            }

            if (context.QModWarnings.Count > 0)
            {
                context.Suggestions.Add(
                    "You have QMod issues (duplicate IDs or missing dependencies). Fix these before investigating deeper conflicts."
                );
            }

            if (context.NautilusWarnings.Count > 0)
            {
                context.Suggestions.Add(
                    "You have potential Nautilus ID/TechType conflicts. Mods sharing the same Id/TechType may not work together."
                );
            }
        }
    }
}
