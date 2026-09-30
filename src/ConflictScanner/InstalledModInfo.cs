using System;
using System.Collections.Generic;

namespace ConflictScanner
{
    public enum ModLoaderType
    {
        BepInEx,
        QMod,
        Patcher,
        Unknown
    }

    public enum ModHealthStatus
    {
        Clean,
        Warning,
        Error
    }

    public class InstalledModInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string GuidOrId { get; set; } = string.Empty;
        public ModLoaderType Loader { get; set; } = ModLoaderType.Unknown;
        public string FolderPath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public int FindingsCount { get; set; }
        public bool HasCriticalOrHigh { get; set; }

        public ModHealthStatus Status =>
            FindingsCount == 0 ? ModHealthStatus.Clean :
            HasCriticalOrHigh ? ModHealthStatus.Error :
            ModHealthStatus.Warning;

        public bool HasIssues => FindingsCount > 0;

        public List<string> AssemblyNames { get; } = new();
        public List<string> Dependencies { get; } = new();
    }
}
