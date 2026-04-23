using System.Collections.Generic;

namespace ConflictScanner
{
    public class ScanContext
    {
        public string GamePath { get; }

        public List<(Severity Level, string Message)> HarmonyWarnings { get; } = new();
        public List<(Severity Level, string Message)> NautilusWarnings { get; } = new();
        public List<(Severity Level, string Message)> QModWarnings { get; } = new();
        public List<(Severity Level, string Message)> FileWarnings { get; } = new();

        public ScanContext(string gamePath)
        {
            GamePath = gamePath;
        }

        public void AddFileWarning(Severity level, string message)
        {
            FileWarnings.Add((level, message));
        }

        public void AddQModWarning(Severity level, string message)
        {
            QModWarnings.Add((level, message));
        }

        public void AddHarmonyWarning(Severity level, string message)
        {
            HarmonyWarnings.Add((level, message));
        }

        public void AddNautilusWarning(Severity level, string message)
        {
            NautilusWarnings.Add((level, message));
        }
    }
}
