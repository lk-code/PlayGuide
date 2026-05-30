using System.Text.Json;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Cache;

/// <summary>
/// Stores the discovered-games cache as JSON in the user's profile.
/// </summary>
/// <param name="paths">Resolves the cache file location.</param>
public sealed class GameCacheService(UserPaths paths) : IGameCacheService
{
    private readonly UserPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));

    /// <inheritdoc />
    public async Task<GameCache> LoadAsync(CancellationToken cancellationToken = default)
    {
        var file = _paths.GameCacheFile;
        if (!File.Exists(file))
        {
            return GameCache.Empty;
        }

        try
        {
            await using var stream = File.OpenRead(file);
            var cache = await JsonSerializer.DeserializeAsync(
                stream, PlayGuideJsonContext.Default.GameCache, cancellationToken).ConfigureAwait(false);
            return cache ?? GameCache.Empty;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // A corrupt or unreadable cache should degrade gracefully to "empty".
            return GameCache.Empty;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(GameCache cache, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _paths.EnsureCreated();
        await using var stream = File.Create(_paths.GameCacheFile);
        await JsonSerializer.SerializeAsync(
            stream, cache, PlayGuideJsonContext.Default.GameCache, cancellationToken).ConfigureAwait(false);
    }
}
