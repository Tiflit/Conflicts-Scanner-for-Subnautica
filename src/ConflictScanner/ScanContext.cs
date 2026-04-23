using System.Collections.Generic;

namespace ConflictScanner
{
    public class ScanContext
    {
        public string GamePath { get; }

        public List<string> HarmonyWarnings { get; } = new();
        public List<string> NautilusWarnings { get; } = new();
        public List<string> QModWarnings { get; } = new();
        public List<string> FileWarnings { get; } = new();

        public ScanContext(string gamePath)
        {
            GamePath = gamePath;
        }
    }
}
