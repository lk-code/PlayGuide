using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Resolves local cover/header artwork for a discovered game. v1 only reads from
/// launchers' local caches; online providers can implement the same contract later.
/// </summary>
public interface IArtworkResolver
{
    /// <summary>The store this resolver handles.</summary>
    StoreKind Store { get; }

    /// <summary>
    /// Resolves a local artwork file path for the given game.
    /// </summary>
    /// <param name="game">The game to resolve artwork for.</param>
    /// <param name="source">The library source the game came from (carries the resolved root path).</param>
    /// <returns>An absolute artwork file path, or <c>null</c> when none was found.</returns>
    string? ResolveArtworkPath(GameEntry game, LibrarySource source);
}
