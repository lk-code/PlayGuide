using System.Collections.Immutable;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Contracts;

/// <summary>
/// Persists and loads the user's configured <see cref="LibrarySource"/> list.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads configured sources. Returns sensible auto-detected defaults when no
    /// settings file exists yet.
    /// </summary>
    Task<ImmutableList<LibrarySource>> LoadSourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the configured sources to disk.</summary>
    Task SaveSourcesAsync(ImmutableList<LibrarySource> sources, CancellationToken cancellationToken = default);
}
