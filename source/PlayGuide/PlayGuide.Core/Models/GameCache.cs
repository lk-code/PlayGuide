using System.Collections.Immutable;

namespace PlayGuide.Core.Models;

/// <summary>
/// The persisted snapshot of discovered games read at application start.
/// </summary>
/// <param name="Version">Schema version, to support future migrations.</param>
/// <param name="UpdatedAt">When the cache was last written.</param>
/// <param name="Games">The cached games.</param>
public record GameCache(
    int Version,
    DateTimeOffset UpdatedAt,
    ImmutableList<GameEntry> Games)
{
    /// <summary>Current cache schema version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>An empty cache at the current schema version.</summary>
    public static GameCache Empty { get; } =
        new(CurrentVersion, DateTimeOffset.MinValue, ImmutableList<GameEntry>.Empty);
}
