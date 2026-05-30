using FluentAssertions;
using PlayGuide.Core.Models;
using PlayGuide.Core.Steam;

namespace PlayGuide.Core.Tests;

[TestClass]
public class SteamLibraryScannerTests
{
    private static void WriteManifest(string steamAppsDir, string appId, string name, string installDir)
    {
        Directory.CreateDirectory(steamAppsDir);
        File.WriteAllText(Path.Combine(steamAppsDir, $"appmanifest_{appId}.acf"), $$"""
            "AppState"
            {
                "appid" "{{appId}}"
                "name" "{{name}}"
                "installdir" "{{installDir}}"
                "LastPlayed" "1700000000"
            }
            """);
    }

    [TestMethod]
    public async Task ScanAsync_DiscoversInstalledGames()
    {
        using var temp = new TempDirectory();
        var steamApps = temp.Combine("steamapps");
        WriteManifest(steamApps, "440", "Team Fortress 2", "Team Fortress 2");
        // Create the install folder so InstallDir is populated.
        Directory.CreateDirectory(Path.Combine(steamApps, "common", "Team Fortress 2"));

        var scanner = new SteamLibraryScanner(new SteamPathLocator());
        var games = await scanner.ScanAsync(new LibrarySource(StoreKind.Steam, temp.Path));

        games.Should().ContainSingle();
        var game = games[0];
        game.Id.Should().Be("steam:440");
        game.Name.Should().Be("Team Fortress 2");
        game.SteamAppId.Should().Be("440");
        game.Store.Should().Be(StoreKind.Steam);
        game.InstallDir.Should().EndWith(Path.Combine("common", "Team Fortress 2"));
        game.LastPlayed.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1700000000));
    }

    [TestMethod]
    public async Task ScanAsync_SkipsRuntimesAndRedistributables()
    {
        using var temp = new TempDirectory();
        var steamApps = temp.Combine("steamapps");
        WriteManifest(steamApps, "228980", "Steamworks Common Redistributables", "Steamworks Shared");
        WriteManifest(steamApps, "1493710", "Proton Experimental", "Proton - Experimental");

        var scanner = new SteamLibraryScanner(new SteamPathLocator());
        var games = await scanner.ScanAsync(new LibrarySource(StoreKind.Steam, temp.Path));

        games.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ScanAsync_EmptyLibraryYieldsNoGames()
    {
        using var temp = new TempDirectory();
        // A valid Steam root (has steamapps) but with no installed games.
        Directory.CreateDirectory(temp.Combine("steamapps"));
        var scanner = new SteamLibraryScanner(new SteamPathLocator());

        var games = await scanner.ScanAsync(new LibrarySource(StoreKind.Steam, temp.Path));

        games.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ScanAsync_IgnoresCorruptManifests()
    {
        using var temp = new TempDirectory();
        var steamApps = temp.Combine("steamapps");
        Directory.CreateDirectory(steamApps);
        File.WriteAllText(Path.Combine(steamApps, "appmanifest_999.acf"), "\"AppState\" { \"appid\" ");
        WriteManifest(steamApps, "440", "Team Fortress 2", "Team Fortress 2");

        var scanner = new SteamLibraryScanner(new SteamPathLocator());
        var games = await scanner.ScanAsync(new LibrarySource(StoreKind.Steam, temp.Path));

        games.Should().ContainSingle().Which.SteamAppId.Should().Be("440");
    }
}
