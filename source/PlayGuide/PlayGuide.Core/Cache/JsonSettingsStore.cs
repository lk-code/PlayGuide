using System.Collections.Immutable;
using System.Text.Json;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;

namespace PlayGuide.Core.Cache;

/// <summary>
/// Stores the user's configured library sources as JSON in the user's profile.
/// </summary>
/// <param name="paths">Resolves the settings file location.</param>
public sealed class JsonSettingsStore(UserPaths paths) : ISettingsStore
{
    private readonly UserPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));

    /// <summary>Default sources used on first run: Steam with auto-detection.</summary>
    private static readonly ImmutableList<LibrarySource> DefaultSources =
        [new LibrarySource(StoreKind.Steam)];

    /// <inheritdoc />
    public async Task<ImmutableList<LibrarySource>> LoadSourcesAsync(CancellationToken cancellationToken = default)
    {
        var file = _paths.SettingsFile;
        if (!File.Exists(file))
        {
            return DefaultSources;
        }

        try
        {
            await using var stream = File.OpenRead(file);
            var sources = await JsonSerializer.DeserializeAsync(
                stream, PlayGuideJsonContext.Default.ImmutableListLibrarySource, cancellationToken)
                .ConfigureAwait(false);
            return sources is { Count: > 0 } ? sources : DefaultSources;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return DefaultSources;
        }
    }

    /// <inheritdoc />
    public async Task SaveSourcesAsync(ImmutableList<LibrarySource> sources, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);

        _paths.EnsureCreated();
        await using var stream = File.Create(_paths.SettingsFile);
        await JsonSerializer.SerializeAsync(
            stream, sources, PlayGuideJsonContext.Default.ImmutableListLibrarySource, cancellationToken)
            .ConfigureAwait(false);
    }
}
