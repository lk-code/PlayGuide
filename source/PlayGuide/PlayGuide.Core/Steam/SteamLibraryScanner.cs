using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Steam;

/// <summary>
/// Discovers installed Steam games by parsing the <c>appmanifest_*.acf</c> files in
/// every Steam library folder. Proton/Windows titles on Linux appear as ordinary
/// app manifests and are therefore covered automatically.
/// </summary>
/// <param name="locator">Locator used to find the Steam root and its libraries.</param>
public sealed class SteamLibraryScanner(SteamPathLocator locator) : ILibraryScanner
{
    private readonly SteamPathLocator _locator =
        locator ?? throw new ArgumentNullException(nameof(locator));

    /// <summary>
    /// App ids that are runtimes/redistributables rather than playable games and are
    /// therefore skipped (Steamworks Common Redistributables and Steam Linux Runtimes).
    /// </summary>
    private static readonly HashSet<string> NonGameAppIds = new(StringComparer.Ordinal)
    {
        "228980",  // Steamworks Common Redistributables
        "1070560", // Steam Linux Runtime 1.0 (scout)
        "1391110", // Steam Linux Runtime 2.0 (soldier)
        "1628350", // Steam Linux Runtime 3.0 (sniper)
        "1493710", // Proton Experimental
    };

    /// <inheritdoc />
    public StoreKind Store => StoreKind.Steam;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameEntry>> ScanAsync(
        LibrarySource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var root = _locator.ResolveRoot(source.CustomPath);
        if (root is null)
        {
            return [];
        }

        var games = new Dictionary<string, GameEntry>(StringComparer.Ordinal);

        foreach (var library in _locator.GetLibraryFolders(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps))
            {
                continue;
            }

            foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var game = await TryParseManifestAsync(manifest, steamApps, cancellationToken)
                    .ConfigureAwait(false);
                if (game is not null)
                {
                    games[game.Id] = game;
                }
            }
        }

        return [.. games.Values];
    }

    private static async Task<GameEntry?> TryParseManifestAsync(
        string manifestPath,
        string steamAppsDir,
        CancellationToken cancellationToken)
    {
        string text;
        try
        {
            text = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            return null;
        }

        VdfNode root;
        try
        {
            root = VdfParser.Parse(text);
        }
        catch (FormatException)
        {
            return null;
        }

        var state = root["AppState"];
        if (state is null)
        {
            return null;
        }

        var appId = state.GetString("appid");
        var name = state.GetString("name");
        if (string.IsNullOrWhiteSpace(appId) ||
            string.IsNullOrWhiteSpace(name) ||
            NonGameAppIds.Contains(appId))
        {
            return null;
        }

        var installDirName = state.GetString("installdir");
        string? installDir = null;
        if (!string.IsNullOrWhiteSpace(installDirName))
        {
            var candidate = Path.Combine(steamAppsDir, "common", installDirName);
            installDir = Directory.Exists(candidate) ? candidate : null;
        }

        return new GameEntry(
            Id: $"steam:{appId}",
            Store: StoreKind.Steam,
            Name: name.Trim(),
            InstallDir: installDir,
            ExecutablePath: null, // Steam games are launched via the steam:// URI.
            SteamAppId: appId,
            ArtworkPath: null, // Resolved later by IArtworkResolver.
            LastPlayed: ParseUnixSeconds(state.GetString("LastPlayed")));
    }

    private static DateTimeOffset? ParseUnixSeconds(string? value) =>
        long.TryParse(value, out var seconds) && seconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}
