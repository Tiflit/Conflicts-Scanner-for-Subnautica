using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace ConflictScanner
{
    public static class GameLocator
    {
        private const string SettingsFileName = "settings.json";

        public static string? LoadSavedPath()
        {
            try
            {
                string settingsPath = GetSettingsPath();
                if (!File.Exists(settingsPath))
                    return null;

                string json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings?.LastGamePath;
            }
            catch
            {
                return null;
            }
        }

        public static void SavePath(string gamePath)
        {
            try
            {
                string folder = GetAppDataFolder();
                Directory.CreateDirectory(folder);

                string settingsPath = GetSettingsPath();
                var settings = new AppSettings
                {
                    LastGamePath = gamePath
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Non-critical.
            }
        }

        public static bool TryAutoLocateSubnautica(out string? path)
        {
            if (TryFindSteamGame(out path))
                return true;

            if (TryFindEpicGame(out path))
                return true;

            path = null;
            return false;
        }

        private static bool TryFindSteamGame(out string? path)
        {
            path = null;

            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                var installPath = key?.GetValue("InstallPath") as string;
                if (string.IsNullOrWhiteSpace(installPath))
                    return false;

                string libraryFolders = Path.Combine(installPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryFolders))
                    return false;

                var libraries = ParseSteamLibraryFolders(libraryFolders);
                // Check Subnautica (264710) then Below Zero (848450)
                string[] appIds = { "264710", "848450" };

                foreach (var appId in appIds)
                {
                    foreach (var lib in libraries)
                    {
                        string manifest = Path.Combine(lib, "steamapps", $"appmanifest_{appId}.acf");
                        if (!File.Exists(manifest))
                            continue;

                        string? installDir = ParseSteamInstallDir(manifest);
                        if (string.IsNullOrWhiteSpace(installDir))
                            continue;

                        string candidate = Path.Combine(lib, "steamapps", "common", installDir);
                        if (Directory.Exists(candidate))
                        {
                            path = candidate;
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryFindEpicGame(out string? path)
        {
            path = null;

            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                string epicManifests = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
                if (!Directory.Exists(epicManifests))
                    return false;

                foreach (var file in Directory.GetFiles(epicManifests, "*.item"))
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains("\"Subnautica\"", StringComparison.OrdinalIgnoreCase))
                    {
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.TryGetProperty("InstallLocation", out var loc))
                        {
                            string? installPath = loc.GetString();
                            if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                            {
                                path = installPath;
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static List<string> ParseSteamLibraryFolders(string vdfPath)
        {
            var result = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(vdfPath);

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("\""))
                        continue;

                    var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    string value = parts[^1];
                    if (Directory.Exists(value))
                        result.Add(value);
                }
            }
            catch
            {
            }

            return result;
        }

        private static string? ParseSteamInstallDir(string acfPath)
        {
            try
            {
                string[] lines = File.ReadAllLines(acfPath);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("\"installdir\""))
                        continue;

                    var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    return parts[^1];
                }
            }
            catch
            {
            }

            return null;
        }

        private static string GetAppDataFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "ConflictScanner");
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(GetAppDataFolder(), SettingsFileName);
        }
    }

    public class AppSettings
    {
        public string? LastGamePath { get; set; }
    }
}
