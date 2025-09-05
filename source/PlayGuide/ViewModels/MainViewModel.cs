using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlayGuide.Core.Contracts;
using PlayGuide.Core.Models;
using System.Collections.ObjectModel;

namespace PlayGuide.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly IGamesProvider _gamesProvider;

    [ObservableProperty]
    private ObservableCollection<Game> games = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public MainViewModel(IGamesProvider gamesProvider)
    {
        _gamesProvider = gamesProvider;
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        IsLoading = true;
        StatusMessage = "Lade installierte Spiele...";

        try
        {
            var installedGames = await _gamesProvider.GetInstalledGamesAsync();
            Games.Clear();

            foreach (var game in installedGames)
            {
                Games.Add(game);
            }

            StatusMessage = $"{Games.Count} Spiele gefunden";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Laden der Spiele: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadGamesAsync();
    }
}
