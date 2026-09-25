using System;
using System.Diagnostics;
using System.IO;

namespace ConflictScanner.Profiles
{
    public enum GameBranch
    {
        Unknown,
        Modern2_0,
        Legacy
    }

    public class GameEnvironmentInfo
    {
        public GameBranch Branch { get; init; } = GameBranch.Unknown;
        public string VersionString { get; init; } = "Unknown";
        public bool HasBepInEx { get; init; }
        public bool HasQMods { get; init; }
    }

    public static class GameEnvironmentDetector
    {
        public static GameEnvironmentInfo Detect(string gamePath)
        {
            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
            {
                return new GameEnvironmentInfo();
            }

            string exe = Path.Combine(gamePath, "Subnautica.exe");
            if (!File.Exists(exe))
                exe = Path.Combine(gamePath, "SubnauticaZero.exe");

            GameBranch branch = GameBranch.Unknown;
            string versionStr = "Unknown";

            if (File.Exists(exe))
            {
                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(exe);
                    versionStr = versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "Unknown";

                    // Build and version heuristics for Living Large 2.0+ vs Legacy
                    if (versionStr.StartsWith("2.") ||
                        versionStr.Contains("2020.") ||
                        versionStr.Contains("2021.") ||
                        versionStr.Contains("2022.") ||
                        versionStr.Contains("2023."))
                    {
                        branch = GameBranch.Modern2_0;
                    }
                    else if (versionStr.StartsWith("1.") || versionStr.Contains("2019."))
                    {
                        branch = GameBranch.Legacy;
                    }
                }
                catch
                {
                    // Non-critical
                }
            }

            bool hasBepInEx = Directory.Exists(Path.Combine(gamePath, "BepInEx"));
            bool hasQMods = Directory.Exists(Path.Combine(gamePath, "QMods"));

            if (branch == GameBranch.Unknown)
            {
                if (hasBepInEx && !hasQMods)
                    branch = GameBranch.Modern2_0;
                else if (hasQMods && !hasBepInEx)
                    branch = GameBranch.Legacy;
            }

            return new GameEnvironmentInfo
            {
                Branch = branch,
                VersionString = versionStr,
                HasBepInEx = hasBepInEx,
                HasQMods = hasQMods
            };
        }
    }
}
