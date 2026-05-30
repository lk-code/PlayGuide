using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Orchestrates the configured <see cref="ILibraryScanner"/> providers: aggregates
/// their results, de-duplicates by <see cref="GameEntry.Id"/>, resolves artwork and
/// persists the result to the cache.
/// </summary>
public interface IGameLibraryService
{
    /// <summary>
    /// Loads the previously cached games (fast path used at startup).
    /// </summary>
    Task<IReadOnlyList<GameEntry>> GetCachedGamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-scans every enabled <see cref="LibrarySource"/> from the settings, merges
    /// and resolves the results, updates the cache and returns the fresh list.
    /// </summary>
    Task<IReadOnlyList<GameEntry>> RefreshAsync(CancellationToken cancellationToken = default);
}
