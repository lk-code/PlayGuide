using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Steam;

/// <summary>
/// Resolves cover/header artwork for Steam games from Steam's local
/// <c>appcache/librarycache</c>. Prefers the tall library capsule (600x900) used
/// for the Vista-style tiles, falling back to header art.
/// </summary>
/// <param name="locator">Locator used to resolve the Steam root from a source.</param>
public sealed class SteamArtworkResolver(SteamPathLocator locator) : IArtworkResolver
{
    private readonly SteamPathLocator _locator =
        locator ?? throw new ArgumentNullException(nameof(locator));

    /// <inheritdoc />
    public StoreKind Store => StoreKind.Steam;

    /// <inheritdoc />
    public string? ResolveArtworkPath(GameEntry game, LibrarySource source)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrEmpty(game.SteamAppId))
        {
            return null;
        }

        var root = _locator.ResolveRoot(source.CustomPath);
        if (root is null)
        {
            return null;
        }

        var cache = Path.Combine(root, "appcache", "librarycache");
        var appId = game.SteamAppId;

        // Candidate layouts, most-preferred first. Newer Steam clients (2023+) use a
        // per-appid sub-folder; older clients use flat "{appid}_*.jpg" files.
        string[] candidates =
        [
            Path.Combine(cache, appId, "library_600x900.jpg"),
            Path.Combine(cache, $"{appId}_library_600x900.jpg"),
            Path.Combine(cache, appId, "library_capsule.jpg"),
            Path.Combine(cache, appId, "header.jpg"),
            Path.Combine(cache, $"{appId}_header.jpg"),
            Path.Combine(cache, appId, "library_hero.jpg"),
        ];

        return Array.Find(candidates, File.Exists);
    }
}
