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
        public ScanMode Mode { get; }
        public TimeSpan ScanDuration { get; set; }

        // Per-system warning lists.
        public List<(Severity Level, string Message)> SMLHelperWarnings { get; } = new();
        public List<(Severity Level, string Message)> HarmonyWarnings    { get; } = new();
        public List<(Severity Level, string Message)> NautilusWarnings   { get; } = new();
        public List<(Severity Level, string Message)> QModWarnings       { get; } = new();
        public List<(Severity Level, string Message)> FileWarnings       { get; } = new();

        // Detected BepInEx patchers (file names / relative paths).
        public List<string> Patchers { get; } = new();

        // General notes to surface in the report.
        public List<string> Notes { get; } = new();

        public List<string> Suggestions { get; } = new();

        public ScanContext(string gamePath, ScanMode mode)
        {
            GamePath = gamePath;
            Mode = mode;
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
