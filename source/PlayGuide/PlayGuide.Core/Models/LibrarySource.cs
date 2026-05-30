namespace PlayGuide.Core.Models;

/// <summary>
/// A game library configured by the user in the settings: a store plus an
/// optional custom installation path used to override automatic detection.
/// </summary>
/// <param name="Store">The store / launcher this source represents.</param>
/// <param name="CustomPath">
/// Optional path to the store's root folder. When <c>null</c>, the scanner
/// auto-detects the location using platform conventions.
/// </param>
/// <param name="Enabled">Whether this source is included in scans.</param>
public record LibrarySource(StoreKind Store, string? CustomPath = null, bool Enabled = true);
