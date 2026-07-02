using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly IPlaybackService _playbackService;

    [ObservableProperty]
    private string searchQuery = "";

    [ObservableProperty]
    private SearchState searchState = SearchState.Idle;

    [ObservableProperty] 
    private string? searchError;
        
    [ObservableProperty]
    private Song? selectedSong;

    [ObservableProperty]
    private Song? currentSong;

    public ObservableCollection<Song> SearchResults { get; } = new();

    public ObservableCollection<Song> Queue { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }

    public IRelayCommand PlaySelectedCommand { get; }
    public IAsyncRelayCommand<Song> PlaySongCommand { get; }

    public MainWindowViewModel(ISearchService searchService, IPlaybackService playbackService)
    {
        _searchService = searchService;
        _playbackService = playbackService;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        
        PlaySelectedCommand = new AsyncRelayCommand(
            PlaySelected,
            () => SelectedSong is not null
        );
        
        PlaySongCommand = new AsyncRelayCommand<Song>(PlaySongAsync);
    }

    partial void OnSelectedSongChanged(Song? value)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    private async Task SearchAsync()
    {
        SearchResults.Clear();
        SearchError = null;
        SearchState = SearchState.Loading;
        try
        {
            var response = await _searchService.SearchAsync(SearchQuery);
            foreach (var song in response.Songs)
            {
                SearchResults.Add(song);
            }

            SearchState = response.isEmpty ? SearchState.Empty : SearchState.Results;
        }
        catch (Exception ex)
        {
            SearchError = ex.Message;
            SearchState = SearchState.Error;
        }
    }

    private Task PlaySelected()
    {
        return PlaySongAsync(SelectedSong);
    }
    
    private async Task PlaySongAsync(Song? song)
    {
        if (song is null)
        {
            return;
        }

        await _playbackService.PlayAsync(song);
        CurrentSong = _playbackService.CurrentSong;

        if (!Queue.Contains(song))
        {
            Queue.Insert(0, song);
        }
    }
}
