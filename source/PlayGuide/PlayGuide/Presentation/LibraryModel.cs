using Uno.Extensions.Navigation;

namespace PlayGuide.Presentation;

/// <summary>
/// MVUX model for the main game library: exposes the (optionally filtered) list of
/// games, a re-scan command and a launch command. The list re-evaluates whenever the
/// search term changes or a re-scan bumps the revision.
/// </summary>
public partial record LibraryModel
{
    private readonly IGameLibraryService _library;
    private readonly IGameLauncher _launcher;
    private readonly INavigator _navigator;
    private bool _autoScanned;

    /// <summary>Initializes the model with its dependencies.</summary>
    public LibraryModel(IGameLibraryService library, IGameLauncher launcher, INavigator navigator)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    /// <summary>Free-text filter applied to game names.</summary>
    public IState<string> SearchTerm => State<string>.Value(this, () => string.Empty);

    /// <summary>Incremented to force a reload after a re-scan.</summary>
    private IState<int> Revision => State<int>.Value(this, () => 0);

    /// <summary>The games shown in the grid, filtered by <see cref="SearchTerm"/>.</summary>
    public IListFeed<GameEntry> Games =>
        Feed.Combine(SearchTerm, Revision)
            .SelectAsync(LoadAsync)
            .AsListFeed();

    private async ValueTask<IImmutableList<GameEntry>> LoadAsync(
        (string Term, int Revision) input,
        CancellationToken ct)
    {
        var games = await _library.GetCachedGamesAsync(ct);

        // First run: the cache is empty, so kick off a scan once so the user sees
        // their installed games without having to trigger a refresh manually.
        if (games.Count == 0 && !_autoScanned && string.IsNullOrWhiteSpace(input.Term))
        {
            _autoScanned = true;
            games = await _library.RefreshAsync(ct);
        }

        IEnumerable<GameEntry> filtered = string.IsNullOrWhiteSpace(input.Term)
            ? games
            : games.Where(g => g.Name.Contains(input.Term.Trim(), StringComparison.OrdinalIgnoreCase));
        return filtered.ToImmutableList();
    }

    /// <summary>Re-scans every configured library and reloads the list.</summary>
    public async ValueTask Refresh(CancellationToken ct)
    {
        await _library.RefreshAsync(ct);
        await Revision.Update(static rev => rev + 1, ct);
    }

    /// <summary>Launches the given game.</summary>
    public async ValueTask LaunchGame(GameEntry game, CancellationToken ct) =>
        await _launcher.LaunchAsync(game, ct);

    /// <summary>Navigates to the settings page.</summary>
    public async ValueTask OpenSettings() =>
        await _navigator.NavigateViewModelAsync<SettingsModel>(this);
}
