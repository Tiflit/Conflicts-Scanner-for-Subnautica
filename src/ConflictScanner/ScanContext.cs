using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConflictScanner.Profiles;

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
        public GameEnvironmentInfo Environment { get; set; }

        public List<Finding> Findings { get; } = new();
        public List<InstalledModInfo> InstalledMods { get; } = new();

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
            Environment = GameEnvironmentDetector.Detect(gamePath);
        }

        public void AddFinding(Finding finding) =>
            Findings.Add(finding);

        public void AddSMLHelperWarning(Severity level, string message)
        {
            SMLHelperWarnings.Add((level, message));
            AddFinding(new Finding
            {
                Category = "SMLHelper",
                Impact = SeverityToImpact(level),
                Confidence = Confidence.Observed,
                Explanation = message,
                Evidence = message
            });
        }

        public void AddHarmonyWarning(Severity level, string message)
        {
            HarmonyWarnings.Add((level, message));
            AddFinding(new Finding
            {
                Category = "Harmony",
                Impact = SeverityToImpact(level),
                Confidence = Confidence.Probable,
                Explanation = message,
                Evidence = message
            });
        }

        public void AddNautilusWarning(Severity level, string message)
        {
            NautilusWarnings.Add((level, message));
            AddFinding(new Finding
            {
                Category = "Nautilus",
                Impact = SeverityToImpact(level),
                Confidence = Confidence.Probable,
                Explanation = message,
                Evidence = message
            });
        }

        public void AddQModWarning(Severity level, string message)
        {
            QModWarnings.Add((level, message));
            AddFinding(new Finding
            {
                Category = "QMod",
                Impact = SeverityToImpact(level),
                Confidence = Confidence.Observed,
                Explanation = message,
                Evidence = message
            });
        }

        public void AddFileWarning(Severity level, string message)
        {
            FileWarnings.Add((level, message));
            AddFinding(new Finding
            {
                Category = "Filesystem",
                Impact = SeverityToImpact(level),
                Confidence = level == Severity.Info ? Confidence.Observed : Confidence.Probable,
                Explanation = message,
                Evidence = message
            });
        }

        public void AddPatcher(string patcher) =>
            Patchers.Add(patcher);

        public void AddNote(string note) =>
            Notes.Add(note);

        public void RegisterOrUpdateMod(InstalledModInfo modInfo)
        {
            var existing = InstalledMods.FirstOrDefault(m =>
                (!string.IsNullOrEmpty(m.GuidOrId) && !string.IsNullOrEmpty(modInfo.GuidOrId) && m.GuidOrId.Equals(modInfo.GuidOrId, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(m.Name) && !string.IsNullOrEmpty(modInfo.Name) && m.Name.Equals(modInfo.Name, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(m.FolderPath) && !string.IsNullOrEmpty(modInfo.FolderPath) && m.FolderPath.Equals(modInfo.FolderPath, StringComparison.OrdinalIgnoreCase)));

            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.Version) && !string.IsNullOrEmpty(modInfo.Version))
                    existing.Version = modInfo.Version;
                if (string.IsNullOrEmpty(existing.GuidOrId) && !string.IsNullOrEmpty(modInfo.GuidOrId))
                    existing.GuidOrId = modInfo.GuidOrId;
                if (existing.Loader == ModLoaderType.Unknown && modInfo.Loader != ModLoaderType.Unknown)
                    existing.Loader = modInfo.Loader;
                foreach (var asm in modInfo.AssemblyNames)
                {
                    if (!existing.AssemblyNames.Contains(asm, StringComparer.OrdinalIgnoreCase))
                        existing.AssemblyNames.Add(asm);
                }
                foreach (var dep in modInfo.Dependencies)
                {
                    if (!existing.Dependencies.Contains(dep, StringComparer.OrdinalIgnoreCase))
                        existing.Dependencies.Add(dep);
                }
            }
            else
            {
                InstalledMods.Add(modInfo);
            }
        }

        public void UpdateModFindings()
        {
            foreach (var mod in InstalledMods)
            {
                var matchingFindings = Findings.Where(f =>
                    f.InvolvedMods.Any(m =>
                        m.Equals(mod.Name, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(mod.GuidOrId) && m.Equals(mod.GuidOrId, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(mod.FolderPath) && m.Equals(Path.GetFileName(mod.FolderPath), StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(mod.GuidOrId) && f.ResourceKey != null && f.ResourceKey.Equals(mod.GuidOrId, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                mod.FindingsCount = matchingFindings.Count;
                mod.HasCriticalOrHigh = matchingFindings.Any(f => f.Impact == Impact.Critical || f.Impact == Impact.High);
            }
        }

        private static Impact SeverityToImpact(Severity severity) => severity switch
        {
            Severity.Info => Impact.Info,
            Severity.Warning => Impact.Medium,
            Severity.Error => Impact.High,
            Severity.Critical => Impact.Critical,
            _ => Impact.Low
        };
    }
}
