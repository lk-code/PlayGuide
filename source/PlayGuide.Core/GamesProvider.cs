using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;
using Microsoft.Win32;
using System.IO;

namespace PlayGuide.Core;

/// <inheritdoc/>
public class GamesProvider : IGamesProvider
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();

        await Task.Run(() =>
        {
            // Durchsuche sowohl 32-bit als auch 64-bit Programme
            games.AddRange(GetProgramsFromRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
            games.AddRange(GetProgramsFromRegistry(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
        });

        // Filtere nur potenzielle Spiele (Programme mit bestimmten Keywords)
        var gameKeywords = new[] { "game", "spiel", "gaming", "steam", "epic", "ubisoft", "ea", "blizzard", "riot", "battle.net" };

        return games.Where(game =>
            gameKeywords.Any(keyword =>
                game.Name.ToLowerInvariant().Contains(keyword) ||
                game.Publisher.ToLowerInvariant().Contains(keyword)
            ) ||
            HasGameExecutable(game.InstallLocation)
        ).ToList();
    }

    private List<Game> GetProgramsFromRegistry(string registryPath)
    {
        var programs = new List<Game>();

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(registryPath);
            if (key == null) return programs;

            foreach (string subkeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var subkey = key.OpenSubKey(subkeyName);
                    if (subkey == null) continue;

                    var displayName = subkey.GetValue("DisplayName") as string;
                    var publisher = subkey.GetValue("Publisher") as string ?? string.Empty;
                    var version = subkey.GetValue("DisplayVersion") as string ?? string.Empty;
                    var installLocation = subkey.GetValue("InstallLocation") as string ?? string.Empty;
                    var displayIcon = subkey.GetValue("DisplayIcon") as string ?? string.Empty;

                    if (!string.IsNullOrEmpty(displayName) &&
                        !displayName.Contains("Microsoft Visual C++") &&
                        !displayName.Contains("Microsoft .NET") &&
                        !displayName.Contains("Windows SDK"))
                    {
                        programs.Add(new Game
                        {
                            Name = displayName,
                            Publisher = publisher,
                            Version = version,
                            InstallLocation = installLocation,
                            IconPath = displayIcon,
                            ExecutablePath = FindGameExecutable(installLocation)
                        });
                    }
                }
                catch
                {
                    // Ignoriere Fehler bei einzelnen Registry-Einträgen
                }
            }
        }
        catch
        {
            // Ignoriere Registry-Zugriffsfehler
        }

        return programs;
    }

    private string FindGameExecutable(string installLocation)
    {
        if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
            return string.Empty;

        try
        {
            var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
            return exeFiles.FirstOrDefault(exe =>
                !Path.GetFileName(exe).ToLowerInvariant().Contains("uninstall") &&
                !Path.GetFileName(exe).ToLowerInvariant().Contains("setup")
            ) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool HasGameExecutable(string installLocation)
    {
        if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
            return false;

        try
        {
            var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
            return exeFiles.Any(exe =>
            {
                var fileName = Path.GetFileName(exe).ToLowerInvariant();
                return !fileName.Contains("uninstall") &&
                       !fileName.Contains("setup") &&
                       !fileName.Contains("updater");
            });
        }
        catch
        {
            return false;
        }
    }
}
