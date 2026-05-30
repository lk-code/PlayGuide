namespace PlayGuide.Core.Models;

/// <summary>
/// An installed game discovered by a library scanner.
/// </summary>
/// <param name="Id">
/// Stable, store-qualified identifier (e.g. <c>"steam:440"</c>). Used to merge
/// and de-duplicate entries across scans.
/// </param>
/// <param name="Store">The store this game belongs to.</param>
/// <param name="Name">Display name of the game.</param>
/// <param name="InstallDir">Absolute path to the game's installation folder, if known.</param>
/// <param name="ExecutablePath">
/// Absolute path to a launchable executable, if one could be resolved. May be
/// <c>null</c> when the game is launched exclusively through a store URI.
/// </param>
/// <param name="SteamAppId">Steam application id, when <see cref="Store"/> is Steam.</param>
/// <param name="ArtworkPath">
/// Absolute path or URI to local cover/header artwork, if available.
/// </param>
/// <param name="LastPlayed">Timestamp of the last known play session, if known.</param>
public record GameEntry(
    string Id,
    StoreKind Store,
    string Name,
    string? InstallDir = null,
    string? ExecutablePath = null,
    string? SteamAppId = null,
    string? ArtworkPath = null,
    DateTimeOffset? LastPlayed = null);
