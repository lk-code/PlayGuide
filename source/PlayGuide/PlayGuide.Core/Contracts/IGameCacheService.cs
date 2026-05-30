using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Persists and loads the <see cref="GameCache"/> in the user's profile.
/// </summary>
public interface IGameCacheService
{
    /// <summary>Loads the cache, returning <see cref="GameCache.Empty"/> when none exists.</summary>
    Task<GameCache> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the cache to disk.</summary>
    Task SaveAsync(GameCache cache, CancellationToken cancellationToken = default);
}
