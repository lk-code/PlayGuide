using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Launches a discovered game using the most appropriate mechanism for its store
/// (store URI or direct executable).
/// </summary>
public interface IGameLauncher
{
    /// <summary>
    /// Launches the given game.
    /// </summary>
    /// <param name="game">The game to launch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> when a launch was initiated.</returns>
    Task<bool> LaunchAsync(GameEntry game, CancellationToken cancellationToken = default);
}
