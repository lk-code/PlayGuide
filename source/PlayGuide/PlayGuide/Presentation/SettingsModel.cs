using Uno.Extensions.Navigation;

namespace PlayGuide.Presentation;

/// <summary>
/// MVUX model for the settings page: manages the configured library sources and lets
/// the user trigger a full re-scan.
/// </summary>
public partial record SettingsModel
{
    private readonly ISettingsStore _store;
    private readonly IGameLibraryService _library;
    private readonly INavigator _navigator;

    /// <summary>Initializes the model with its dependencies.</summary>
    public SettingsModel(ISettingsStore store, IGameLibraryService library, INavigator navigator)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>The configured library sources.</summary>
    public IListState<LibrarySource> Sources =>
        ListState.Async(this, async ct => (IImmutableList<LibrarySource>)await _store.LoadSourcesAsync(ct));

    /// <summary>
    /// Optional Steam path to add. Leave empty to let PlayGuide auto-detect the
    /// installation location for the current platform.
    /// </summary>
    public IState<string> NewSteamPath => State<string>.Value(this, () => string.Empty);

    /// <summary>Adds a Steam library source using <see cref="NewSteamPath"/>.</summary>
    public async ValueTask AddSteamLibrary(CancellationToken ct)
    {
        var path = (await NewSteamPath)?.Trim();
        var source = new LibrarySource(StoreKind.Steam, string.IsNullOrWhiteSpace(path) ? null : path);

        await Sources.UpdateAsync(list => list.Contains(source) ? list : list.Add(source), ct);
        await NewSteamPath.Set(string.Empty, ct);
        await PersistAsync(ct);
    }

    /// <summary>Removes a configured source.</summary>
    public async ValueTask RemoveSource(LibrarySource source, CancellationToken ct)
    {
        await Sources.UpdateAsync(list => list.Remove(source), ct);
        await PersistAsync(ct);
    }

    /// <summary>Persists the sources, runs a full re-scan and returns to the library.</summary>
    public async ValueTask ScanNow(CancellationToken ct)
    {
        await PersistAsync(ct);
        await _library.RefreshAsync(ct);
        await _navigator.NavigateBackAsync(this);
    }

    /// <summary>Returns to the library without re-scanning.</summary>
    public async ValueTask GoBack() => await _navigator.NavigateBackAsync(this);

    private async ValueTask PersistAsync(CancellationToken ct)
    {
        var current = await Sources;
        await _store.SaveSourcesAsync(current.ToImmutableList(), ct);
    }
}
