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
    private readonly IQueueService _queueService;

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
    
    public IAsyncRelayCommand PauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }
    public IAsyncRelayCommand ResumeCommand {get; }
    
    public IAsyncRelayCommand<Song> PlaySongCommand { get; }

    public MainWindowViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        IQueueService queueService)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _queueService = queueService;

        SearchCommand = new AsyncRelayCommand(SearchAsync);

        PlaySelectedCommand = new AsyncRelayCommand(
            PlaySelected,
            () => SelectedSong is not null
        );

        PlaySongCommand = new AsyncRelayCommand<Song>(PlaySongAsync);

        PauseCommand = new AsyncRelayCommand(PauseAsync);
        ResumeCommand = new AsyncRelayCommand(ResumeAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        NextCommand = new AsyncRelayCommand(NextAsync);
        PreviousCommand = new AsyncRelayCommand(PreviousAsync);
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

            SearchState = response.IsEmpty ? SearchState.Empty : SearchState.Results;
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

        _queueService.PlayNow(song);
        SyncQueueFromService();
    }

    private void SyncQueueFromService()
    {
        Queue.Clear();
        
        foreach (var song in SearchResults)
        {
            Queue.Add(song);
        }
    }
    
    private async Task PauseAsync()
    {
        await _playbackService.PauseAsync();
    }

    private async Task ResumeAsync()
    {
        await _playbackService.ResumeAsync();
    }

    private async Task StopAsync()
    {
        await _playbackService.StopAsync();
        CurrentSong = _playbackService.CurrentSong;
    }

    private async Task NextAsync()
    {
        var next = _queueService.MoveNext();

        if (next is null)
        {
            return;
        }

        await _playbackService.PlayAsync(next);
        CurrentSong = _playbackService.CurrentSong;
        SyncQueueFromService();
    }

    private async Task PreviousAsync()
    {
        var previous = _queueService.MovePrevious();

        if (previous is null)
        {
            return;
        }

        await _playbackService.PlayAsync(previous);
        CurrentSong = _playbackService.CurrentSong;
        SyncQueueFromService();
    }
}
