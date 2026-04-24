using System;
using System.Collections.Generic;

namespace ConflictScanner
{
    public enum ScanMode
    {
        Quick,
        Deep
    }

    public class ScanContext
    {
        public string GamePath { get; }
        public string GameName { get; }
        public ScanMode Mode { get; }
        public TimeSpan ScanDuration { get; set; }

        public List<(Severity Level, string Message)> SMLHelperWarnings { get; } = new();
        public List<(Severity Level, string Message)> HarmonyWarnings    { get; } = new();
        public List<(Severity Level, string Message)> NautilusWarnings   { get; } = new();
        public List<(Severity Level, string Message)> QModWarnings       { get; } = new();
        public List<(Severity Level, string Message)> FileWarnings       { get; } = new();

        public List<string> Patchers { get; } = new();
        public List<string> Notes { get; } = new();
        public List<string> Suggestions { get; } = new();

        public ScanContext(string gamePath, ScanMode mode, string gameName)
        {
            GamePath = gamePath;
            Mode = mode;
            GameName = gameName;
        }

        public void AddSMLHelperWarning(Severity level, string message) =>
            SMLHelperWarnings.Add((level, message));

        public void AddHarmonyWarning(Severity level, string message) =>
            HarmonyWarnings.Add((level, message));

        public void AddNautilusWarning(Severity level, string message) =>
            NautilusWarnings.Add((level, message));

        public void AddQModWarning(Severity level, string message) =>
            QModWarnings.Add((level, message));

        public void AddFileWarning(Severity level, string message) =>
            FileWarnings.Add((level, message));

        public void AddPatcher(string patcher) =>
            Patchers.Add(patcher);

        public void AddNote(string note) =>
            Notes.Add(note);
    }
}
