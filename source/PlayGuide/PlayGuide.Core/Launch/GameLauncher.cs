using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Launch;

/// <summary>
/// Launches games via the appropriate mechanism: a store URI (e.g.
/// <c>steam://rungameid/{appid}</c>) when available, otherwise the resolved
/// executable. URIs and executables are opened through the OS shell so the
/// behaviour is consistent across Windows, Linux and macOS.
/// </summary>
/// <param name="logger">Diagnostics logger.</param>
public sealed class GameLauncher(ILogger<GameLauncher> logger) : IGameLauncher
{
    private readonly ILogger<GameLauncher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task<bool> LaunchAsync(GameEntry game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);

        var target = ResolveLaunchTarget(game);
        if (target is null)
        {
            _logger.LogWarning("No launch target could be resolved for game {GameName} ({GameId}).",
                game.Name, game.Id);
            return Task.FromResult(false);
        }

        try
        {
            OpenWithShell(target);
            _logger.LogInformation("Launched {GameName} via {Target}.", game.Name, target);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch {GameName} via {Target}.", game.Name, target);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Determines the launch target for a game: a store URI when known, else the
    /// executable path. Exposed for testing.
    /// </summary>
    public static string? ResolveLaunchTarget(GameEntry game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.Store == StoreKind.Steam && !string.IsNullOrEmpty(game.SteamAppId))
        {
            return $"steam://rungameid/{game.SteamAppId}";
        }

        return string.IsNullOrWhiteSpace(game.ExecutablePath) ? null : game.ExecutablePath;
    }

    private static void OpenWithShell(string target)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // UseShellExecute resolves both URIs and executables on Windows.
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", [target]);
        }
        else
        {
            Process.Start("xdg-open", [target]);
        }
    }
}
