using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using App.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private readonly IQueueService _queueService;
    private readonly ISearchService _searchService;
    private readonly ICollectionService _collectionService;

    [ObservableProperty] private Song? currentSong;

    [ObservableProperty] private string? searchError;

    [ObservableProperty] private string searchQuery = "";

    [ObservableProperty] private SearchState searchState = SearchState.Idle;

    [ObservableProperty] private Song? selectedSong;

    [ObservableProperty] private MainContentView currentView = MainContentView.Search;

    public MainWindowViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        IQueueService queueService,
        ICollectionService collectionService)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _queueService = queueService;
        _collectionService = collectionService;

        SearchCommand = new AsyncRelayCommand(SearchAsync);

        PlaySelectedCommand = new AsyncRelayCommand(
            PlaySelected,
            () => SelectedSong is not null
        );

        PlaySongCommand = new AsyncRelayCommand<Song>(PlaySongAsync);

        OpenAlbumCommand = new AsyncRelayCommand<Album>(OpenAlbumAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist>(OpenPlaylistAsync);
        OpenArtistCommand = new AsyncRelayCommand<Artist>(OpenArtistAsync);
        BackToSearchCommand = new RelayCommand(BackToSearch);

        PauseCommand = new AsyncRelayCommand(PauseAsync);
        ResumeCommand = new AsyncRelayCommand(ResumeAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        NextCommand = new AsyncRelayCommand(NextAsync);
        PreviousCommand = new AsyncRelayCommand(PreviousAsync);
    }

    public ObservableCollection<Song> SearchResults { get; } = new();

    public ObservableCollection<Song> SearchSongs { get; } = new();
    public ObservableCollection<Album> SearchAlbums { get; } = new();
    public ObservableCollection<Artist> SearchArtists { get; } = new();
    public ObservableCollection<Playlist> SearchPlaylists { get; } = new();

    public bool HasSongs => SearchSongs.Count > 0;
    public bool HasAlbums => SearchAlbums.Count > 0;
    public bool HasArtists => SearchArtists.Count > 0;
    public bool HasPlaylists => SearchPlaylists.Count > 0;

    public CollectionDetailViewModel CollectionDetail { get; } = new();
    public ArtistDetailViewModel ArtistDetail { get; } = new();

    public bool IsSearchView => CurrentView == MainContentView.Search;
    public bool IsCollectionDetailView => CurrentView == MainContentView.CollectionDetail;
    public bool IsArtistDetailView => CurrentView == MainContentView.ArtistDetail;

    public ObservableCollection<Song> Queue { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }

    public IRelayCommand PlaySelectedCommand { get; }

    public IAsyncRelayCommand PauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }
    public IAsyncRelayCommand ResumeCommand { get; }

    public IAsyncRelayCommand<Song> PlaySongCommand { get; }

    public IAsyncRelayCommand<Album> OpenAlbumCommand { get; }
    public IAsyncRelayCommand<Playlist> OpenPlaylistCommand { get; }
    public IAsyncRelayCommand<Artist> OpenArtistCommand { get; }
    public IRelayCommand BackToSearchCommand { get; }

    partial void OnCurrentViewChanged(MainContentView value)
    {
        OnPropertyChanged(nameof(IsSearchView));
        OnPropertyChanged(nameof(IsCollectionDetailView));
        OnPropertyChanged(nameof(IsArtistDetailView));
    }

    partial void OnSelectedSongChanged(Song? value)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    private void ClearSearchResults()
    {
        SearchSongs.Clear();
        SearchAlbums.Clear();
        SearchArtists.Clear();
        SearchPlaylists.Clear();
    }

    private void NotifySearchSectionChanges()
    {
        OnPropertyChanged(nameof(HasSongs));
        OnPropertyChanged(nameof(HasAlbums));
        OnPropertyChanged(nameof(HasArtists));
        OnPropertyChanged(nameof(HasPlaylists));
    }

    private async Task SearchAsync()
    {
        ClearSearchResults();
        NotifySearchSectionChanges();

        SearchError = null;
        SearchState = SearchState.Loading;

        try
        {
            var response = await _searchService.SearchAsync(SearchQuery);

            foreach (var song in response.Songs) SearchSongs.Add(song);
            foreach (var album in response.Albums) SearchAlbums.Add(album);
            foreach (var artist in response.Artists) SearchArtists.Add(artist);
            foreach (var playlist in response.Playlists) SearchPlaylists.Add(playlist);

            NotifySearchSectionChanges();

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
        if (song is null) return;

        await _playbackService.PlayAsync(song);
        CurrentSong = _playbackService.CurrentSong;

        _queueService.PlayNow(song);
        SyncQueueFromService();
    }

    private void SyncQueueFromService()
    {
        Queue.Clear();

        foreach (var song in _queueService.Items) Queue.Add(song);
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

        if (next is null) return;

        await _playbackService.PlayAsync(next);
        CurrentSong = _playbackService.CurrentSong;
        SyncQueueFromService();
    }

    private async Task PreviousAsync()
    {
        var previous = _queueService.MovePrevious();

        if (previous is null) return;

        await _playbackService.PlayAsync(previous);
        CurrentSong = _playbackService.CurrentSong;
        SyncQueueFromService();
    }

    private void BackToSearch()
    {
        CurrentView = MainContentView.Search;
    }

    private async Task OpenAlbumAsync(Album? album)
    {
        if (album is null)
        {
            return;
        }

        CollectionDetail.LoadAlbum(album);
        CurrentView = MainContentView.CollectionDetail;

        try
        {
            CollectionDetail.IsLoading = true;
            CollectionDetail.Error = null;

            var detail = await _collectionService.GetAlbumAsync(album);
            CollectionDetail.LoadDetail(detail);
        }
        catch (Exception ex)
        {
            CollectionDetail.Error = ex.Message;
        }
        finally
        {
            CollectionDetail.IsLoading = false;
        }
    }

    private async Task OpenPlaylistAsync(Playlist? playlist)
    {
        if (playlist is null)
        {
            return;
        }

        CollectionDetail.LoadPlaylist(playlist);
        CurrentView = MainContentView.CollectionDetail;

        try
        {
            CollectionDetail.IsLoading = true;
            CollectionDetail.Error = null;

            var detail = await _collectionService.GetPlaylistAsync(playlist);
            CollectionDetail.LoadDetail(detail);
        }
        catch (Exception ex)
        {
            CollectionDetail.Error = ex.Message;
        }
        finally
        {
            CollectionDetail.IsLoading = false;
        }
    }

    private async Task OpenArtistAsync(Artist? artist)
    {
        if (artist is null)
        {
            return;
        }

        ArtistDetail.LoadArtist(artist);
        CurrentView = MainContentView.ArtistDetail;

        try
        {
            ArtistDetail.IsLoading = true;
            ArtistDetail.Error = null;

            var detail = await _collectionService.GetArtistAsync(artist);
            ArtistDetail.LoadDetail(detail);
        }
        catch (Exception ex)
        {
            ArtistDetail.Error = ex.Message;
        }
        finally
        {
            ArtistDetail.IsLoading = false;
        }
    }
}