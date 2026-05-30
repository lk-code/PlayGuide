using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Library;

/// <summary>
/// Default <see cref="IGameLibraryService"/>: runs the registered scanners for every
/// enabled source, resolves artwork, merges by id and persists the result.
/// </summary>
/// <param name="settings">Provides the configured library sources.</param>
/// <param name="scanners">The per-store scanners.</param>
/// <param name="artworkResolvers">The per-store artwork resolvers.</param>
/// <param name="cache">Persists the merged result.</param>
/// <param name="logger">Diagnostics logger.</param>
public sealed class GameLibraryService(
    ISettingsStore settings,
    IEnumerable<ILibraryScanner> scanners,
    IEnumerable<IArtworkResolver> artworkResolvers,
    IGameCacheService cache,
    ILogger<GameLibraryService> logger) : IGameLibraryService
{
    private readonly ISettingsStore _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IGameCacheService _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<GameLibraryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly Dictionary<StoreKind, ILibraryScanner> _scanners =
        (scanners ?? throw new ArgumentNullException(nameof(scanners))).ToDictionary(s => s.Store);

    private readonly Dictionary<StoreKind, IArtworkResolver> _artworkResolvers =
        (artworkResolvers ?? throw new ArgumentNullException(nameof(artworkResolvers))).ToDictionary(r => r.Store);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameEntry>> GetCachedGamesAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.LoadAsync(cancellationToken).ConfigureAwait(false);
        return cached.Games;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameEntry>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _settings.LoadSourcesAsync(cancellationToken).ConfigureAwait(false);
        var merged = new Dictionary<string, GameEntry>(StringComparer.Ordinal);

        foreach (var source in sources.Where(s => s.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_scanners.TryGetValue(source.Store, out var scanner))
            {
                _logger.LogDebug("No scanner registered for store {Store}; skipping.", source.Store);
                continue;
            }

            IReadOnlyList<GameEntry> found;
            try
            {
                found = await scanner.ScanAsync(source, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scanning {Store} failed.", source.Store);
                continue;
            }

            _artworkResolvers.TryGetValue(source.Store, out var resolver);
            foreach (var game in found)
            {
                var resolved = resolver is null
                    ? game
                    : game with { ArtworkPath = resolver.ResolveArtworkPath(game, source) };
                merged[resolved.Id] = resolved;
            }

            _logger.LogInformation("Found {Count} games in {Store}.", found.Count, source.Store);
        }

        var games = merged.Values
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToImmutableList();

        await _cache.SaveAsync(
            new GameCache(GameCache.CurrentVersion, DateTimeOffset.UtcNow, games), cancellationToken)
            .ConfigureAwait(false);

        return games;
    }
}
