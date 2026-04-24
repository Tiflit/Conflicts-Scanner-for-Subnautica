namespace ConflictScanner
{
    /// <summary>
    /// A single line in the structured report, carrying severity for color-coding.
    /// </summary>
    public class ReportEntry
    {
        public Severity? Level { get; }        // null = plain header/note line
        public string Category { get; }
        public string Message { get; }
        public bool IsHeader { get; }

        private ReportEntry(Severity? level, string category, string message, bool isHeader)
        {
            Level    = level;
            Category = category;
            Message  = message;
            IsHeader = isHeader;
        }

        public static ReportEntry Header(string text)
            => new(null, string.Empty, text, isHeader: true);

        public static ReportEntry Plain(string text)
            => new(null, string.Empty, text, isHeader: false);

        public static ReportEntry Warning(Severity level, string category, string message)
            => new(level, category, message, isHeader: false);
    }
}
