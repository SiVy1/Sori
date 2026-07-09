using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
    private readonly IPlaybackCoordinator _playbackCoordinator;
    private readonly IQueueService _queueService;
    private readonly IPrefetchingPlaybackResolver? _prefetchResolver;
    private readonly ISearchService _searchService;
    private readonly ICollectionService _collectionService;
    private readonly IHomeService _homeService;

    [ObservableProperty] private Song? currentSong;

    [ObservableProperty] private PlaybackSnapshot currentPlaybackSnapshot = new();

    [ObservableProperty] private string? playbackError;

    public bool HasPlaybackError => !string.IsNullOrWhiteSpace(PlaybackError);

    public string CurrentTrackTitle =>
        CurrentPlaybackSnapshot.CurrentTrack?.Title ?? "Nothing playing";

    public string CurrentTrackArtist =>
        CurrentPlaybackSnapshot.CurrentTrack?.ArtistName ?? "";

    public string PlaybackStatusText =>
        CurrentPlaybackSnapshot.State.ToString();

    [ObservableProperty] private double playbackPositionSeconds;

    [ObservableProperty] private double playbackDurationSeconds;

    public string PlaybackPositionText => FormatTime(TimeSpan.FromSeconds(PlaybackPositionSeconds));

    public string PlaybackDurationText =>
        PlaybackDurationSeconds > 0
            ? FormatTime(TimeSpan.FromSeconds(PlaybackDurationSeconds))
            : "--:--";

    public bool CanSeek => PlaybackDurationSeconds > 0;

    public bool IsPlaying => CurrentPlaybackSnapshot.State == PlaybackState.Playing;

    public bool ShuffleEnabled => _queueService.ShuffleEnabled;

    public RepeatMode RepeatMode => _queueService.RepeatMode;

    public string RepeatModeText => RepeatMode switch
    {
        RepeatMode.Off => "Repeat Off",
        RepeatMode.All => "Repeat All",
        RepeatMode.One => "Repeat One",
        _ => "Repeat"
    };

    public string ShuffleText => ShuffleEnabled ? "Shuffle On" : "Shuffle Off";

    public bool CanGoNext => _queueService.Items.Count > 0;

    public bool CanGoPrevious => _queueService.Items.Count > 0;

    [ObservableProperty] private double playbackVolumePercent = 100.0;

    [ObservableProperty] private bool isPlaybackLoading;

    [ObservableProperty] private int queueCurrentIndex = -1;

    public string QueueIndexText =>
        QueueCurrentIndex >= 0 && _queueService.Items.Count > 0
            ? $"{QueueCurrentIndex + 1} / {_queueService.Items.Count}"
            : "";

    private bool _isUpdatingPlaybackPosition;
    private CancellationTokenSource? _seekDebounceCts;

    [ObservableProperty] private string? searchError;

    [ObservableProperty] private string searchQuery = "";

    [ObservableProperty] private string commandQuery = "";

    [ObservableProperty] private bool isCommandMode;

    [ObservableProperty] private SearchState searchState = SearchState.Idle;

    [ObservableProperty] private Song? selectedSong;

    [ObservableProperty] private MainContentView currentView = MainContentView.Home;

    [ObservableProperty] private bool isModalOpen;

    [ObservableProperty] private bool canOpenModal = true;

    [ObservableProperty] private bool isHomeLoading;

    [ObservableProperty] private string? homeError;

    public MainWindowViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        IQueueService queueService,
        ICollectionService collectionService,
        IHomeService homeService,
        IPlaybackCoordinator playbackCoordinator,
        IPrefetchingPlaybackResolver? prefetchResolver = null)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _queueService = queueService;
        _collectionService = collectionService;
        _homeService = homeService;
        _playbackCoordinator = playbackCoordinator;
        _prefetchResolver = prefetchResolver;

        _playbackCoordinator.SnapshotChanged += (_, args) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentPlaybackSnapshot = args.Snapshot;
            });
        };

        _queueService.Changed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SyncQueueFromService();
                NotifyQueuePropertiesChanged();
            });
        };

        SearchCommand = new AsyncRelayCommand(SearchAsync);

        PlaySelectedCommand = new AsyncRelayCommand(
            PlaySelected,
            () => SelectedSong is not null
        );

        PlaySongCommand = new AsyncRelayCommand<Song>(song => PlaySongAsync(song, null));

        OpenAlbumCommand = new AsyncRelayCommand<Album>(OpenAlbumAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist>(OpenPlaylistAsync);
        OpenArtistCommand = new AsyncRelayCommand<Artist>(OpenArtistAsync);
        OpenHomeItemCommand = new AsyncRelayCommand<HomeItem>(OpenHomeItemAsync);
        BackToSearchCommand = new RelayCommand(BackToSearch);
        ToggleModalCommand = new RelayCommand(ToggleModal);
        ExecuteCommandCommand = new RelayCommand(ExecuteCommand);
        LoadHomeCommand = new AsyncRelayCommand(LoadHomeAsync);

        TogglePlayPauseCommand = new AsyncRelayCommand(TogglePlayPauseAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        NextCommand = new AsyncRelayCommand(NextAsync);
        PreviousCommand = new AsyncRelayCommand(PreviousAsync);

        ToggleShuffleCommand = new RelayCommand(ToggleShuffle);
        CycleRepeatModeCommand = new RelayCommand(CycleRepeatMode);
        PlayCollectionCommand = new AsyncRelayCommand(PlayCollectionAsync);
        PlayCollectionTrackCommand = new AsyncRelayCommand<Song>(PlayCollectionTrackAsync);

        _ = LoadHomeAsync();
    }

    public ObservableCollection<Song> SearchResults { get; } = new();

    public ObservableCollection<Song> SearchSongs { get; } = new();
    public ObservableCollection<Album> SearchAlbums { get; } = new();
    public ObservableCollection<Artist> SearchArtists { get; } = new();
    public ObservableCollection<Playlist> SearchPlaylists { get; } = new();

    public ObservableCollection<HomeItem> ModalItems { get; } = new();

    [ObservableProperty] private int modalSelectedIndex = -1;

    public bool HasSongs => SearchSongs.Count > 0;
    public bool HasAlbums => SearchAlbums.Count > 0;
    public bool HasArtists => SearchArtists.Count > 0;
    public bool HasPlaylists => SearchPlaylists.Count > 0;

    public CollectionDetailViewModel CollectionDetail { get; } = new();
    public ArtistDetailViewModel ArtistDetail { get; } = new();

    public bool IsHomeView => CurrentView == MainContentView.Home;
    public bool IsCollectionDetailView => CurrentView == MainContentView.CollectionDetail;
    public bool IsArtistDetailView => CurrentView == MainContentView.ArtistDetail;

    public ObservableCollection<HomeSection> HomeSections { get; } = new();

    public ObservableCollection<Song> Queue { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }

    public IRelayCommand PlaySelectedCommand { get; }

    public IAsyncRelayCommand TogglePlayPauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }

    public IRelayCommand ToggleShuffleCommand { get; }
    public IRelayCommand CycleRepeatModeCommand { get; }
    public IAsyncRelayCommand PlayCollectionCommand { get; }
    public IAsyncRelayCommand<Song> PlayCollectionTrackCommand { get; }

    public IAsyncRelayCommand<Song> PlaySongCommand { get; }

    public IAsyncRelayCommand<Album> OpenAlbumCommand { get; }
    public IAsyncRelayCommand<Playlist> OpenPlaylistCommand { get; }
    public IAsyncRelayCommand<Artist> OpenArtistCommand { get; }
    public IAsyncRelayCommand<HomeItem> OpenHomeItemCommand { get; }
    public IRelayCommand BackToSearchCommand { get; }
    public IRelayCommand ToggleModalCommand { get; }
    public IRelayCommand ExecuteCommandCommand { get; }
    public IAsyncRelayCommand LoadHomeCommand { get; }

    partial void OnCurrentViewChanged(MainContentView value)
    {
        OnPropertyChanged(nameof(IsHomeView));
        OnPropertyChanged(nameof(IsCollectionDetailView));
        OnPropertyChanged(nameof(IsArtistDetailView));
    }

    partial void OnSelectedSongChanged(Song? value)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentPlaybackSnapshotChanged(PlaybackSnapshot value)
    {
        _isUpdatingPlaybackPosition = true;

        PlaybackPositionSeconds = value.Position.TotalSeconds;
        PlaybackDurationSeconds = value.Duration?.TotalSeconds ?? 0;
        PlaybackVolumePercent = Math.Clamp(value.Volume * 100.0, 0, 100);

        _isUpdatingPlaybackPosition = false;

        OnPropertyChanged(nameof(CurrentTrackTitle));
        OnPropertyChanged(nameof(CurrentTrackArtist));
        OnPropertyChanged(nameof(PlaybackStatusText));
        OnPropertyChanged(nameof(PlaybackPositionText));
        OnPropertyChanged(nameof(PlaybackDurationText));
        OnPropertyChanged(nameof(CanSeek));
        OnPropertyChanged(nameof(IsPlaying));
    }

    partial void OnPlaybackPositionSecondsChanged(double value)
    {
        if (_isUpdatingPlaybackPosition) return;

        _seekDebounceCts?.Cancel();
        _seekDebounceCts = new CancellationTokenSource();
        var token = _seekDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!token.IsCancellationRequested)
                {
                    await _playbackCoordinator.SeekAsync(TimeSpan.FromSeconds(value));
                }
            }
            catch (OperationCanceledException)
            {
                // debounce cancelled
            }
        }, token);
    }

    partial void OnPlaybackVolumePercentChanged(double value)
    {
        if (_isUpdatingPlaybackPosition) return;

        _ = _playbackCoordinator.SetVolumeAsync(value / 100.0);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{time.Hours:D1}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    partial void OnPlaybackErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPlaybackError));
    }

    partial void OnSearchQueryChanged(string value)
    {
        IsCommandMode = value.StartsWith(">");
        CommandQuery = IsCommandMode ? value[1..].TrimStart() : "";
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
        ModalItems.Clear();
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

            foreach (var song in response.Songs)
                ModalItems.Add(new HomeItem { Kind = HomeItemKind.Song, Song = song });
            foreach (var album in response.Albums)
                ModalItems.Add(new HomeItem { Kind = HomeItemKind.Album, Album = album });
            foreach (var artist in response.Artists)
                ModalItems.Add(new HomeItem { Kind = HomeItemKind.Artist, Artist = artist });
            foreach (var playlist in response.Playlists)
                ModalItems.Add(new HomeItem { Kind = HomeItemKind.Playlist, Playlist = playlist });

            ModalSelectedIndex = ModalItems.Count > 0 ? 0 : -1;

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
        return PlaySongAsync(SelectedSong, SearchSongs);
    }

    private async Task PlaySongAsync(Song? song, IEnumerable<Song>? contextSongs = null)
    {
        if (song is null) return;

        _prefetchResolver?.Clear();

        if (contextSongs is not null)
        {
            _queueService.SetContext(contextSongs, song);
        }
        else if (_queueService.Items.All(x => x.Id != song.Id))
        {
            _queueService.PlayNow(song);
        }

        SyncQueueFromService();
        CurrentSong = song;

        await PlayCurrentQueueItemAsync();
    }

    private async Task PlayCurrentQueueItemAsync()
    {
        var current = _queueService.Current;

        if (current is null)
        {
            return;
        }

        try
        {
            PlaybackError = null;
            IsPlaybackLoading = true;
            await _playbackCoordinator.PlaySongAsync(current);

            PrefetchNextQueueItem();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
            // Do NOT remove from queue on playback error.
            // The user may want to retry or skip manually.
            if (CurrentSong == current) CurrentSong = null;
        }
        finally
        {
            IsPlaybackLoading = false;
        }
    }

    private void PrefetchNextQueueItem()
    {
        if (_prefetchResolver is null)
        {
            return;
        }

        var next = _queueService.PeekNext();

        if (next is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _prefetchResolver.PrefetchAsync(next);
            }
            catch
            {
                // Prefetch failure must never break playback.
            }
        });
    }

    private void SyncQueueFromService()
    {
        Queue.Clear();

        foreach (var song in _queueService.Items) Queue.Add(song);

        var current = _queueService.Current;
        if (current is not null)
        {
            var idx = Queue.Select((s, i) => (s, i)).FirstOrDefault(x => x.s.Id == current.Id).i;
            QueueCurrentIndex = idx;
        }
        else
        {
            QueueCurrentIndex = -1;
        }

        OnPropertyChanged(nameof(QueueIndexText));
    }

    private void NotifyQueuePropertiesChanged()
    {
        OnPropertyChanged(nameof(QueueCurrentIndex));
        OnPropertyChanged(nameof(QueueIndexText));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(ShuffleEnabled));
        OnPropertyChanged(nameof(RepeatMode));
        OnPropertyChanged(nameof(RepeatModeText));
        OnPropertyChanged(nameof(ShuffleText));
    }

    private void ToggleShuffle()
    {
        _queueService.ToggleShuffle();
        NotifyQueuePropertiesChanged();
    }

    private void CycleRepeatMode()
    {
        _queueService.CycleRepeatMode();
        NotifyQueuePropertiesChanged();
    }

    private async Task PlayCollectionAsync()
    {
        var tracks = CollectionDetail.Tracks.ToList();

        if (tracks.Count == 0)
        {
            return;
        }

        _prefetchResolver?.Clear();
        _queueService.SetQueue(tracks, 0);
        SyncQueueFromService();
        NotifyQueuePropertiesChanged();

        await PlayCurrentQueueItemAsync();
    }

    private async Task PlayCollectionTrackAsync(Song? song)
    {
        if (song is null)
        {
            return;
        }

        var tracks = CollectionDetail.Tracks.ToList();
        var index = tracks.FindIndex(x => x.Id == song.Id);

        if (index < 0)
        {
            tracks = [song];
            index = 0;
        }

        _prefetchResolver?.Clear();
        _queueService.SetQueue(tracks, index);
        SyncQueueFromService();
        NotifyQueuePropertiesChanged();

        await PlayCurrentQueueItemAsync();
    }

    private async Task TogglePlayPauseAsync()
    {
        try
        {
            PlaybackError = null;
            if (IsPlaying)
                await _playbackCoordinator.PauseAsync();
            else
                await _playbackCoordinator.ResumeAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
    }

    private async Task StopAsync()
    {
        try
        {
            PlaybackError = null;
            await _playbackCoordinator.StopAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
    }

    private async Task NextAsync()
    {
        var next = _queueService.MoveNext();

        if (next is null) return;

        SyncQueueFromService();
        NotifyQueuePropertiesChanged();

        await PlayCurrentQueueItemAsync();
    }

    private async Task PreviousAsync()
    {
        var previous = _queueService.MovePrevious();

        if (previous is null) return;

        SyncQueueFromService();
        NotifyQueuePropertiesChanged();

        await PlayCurrentQueueItemAsync();
    }

    private void BackToSearch()
    {
        CurrentView = MainContentView.Home;
    }

    private void ToggleModal()
    {
        if (!IsModalOpen && !CanOpenModal) return;
        IsModalOpen = !IsModalOpen;
    }

    public async Task StartModalCooldown()
    {
        CanOpenModal = false;
        await Task.Delay(800);
        CanOpenModal = true;
    }

    private void ExecuteCommand()
    {
        var cmd = CommandQuery.Trim().ToLowerInvariant();

        switch (cmd)
        {
            case "home":
                CurrentView = MainContentView.Home;
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
            case "search":
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
            case "queue":
                // ponytail: queue visibility toggle not implemented yet
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
            case "play" or "pause":
                _ = PlaySelected();
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
            case "next":
                _ = NextAsync();
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
            case "prev" or "previous":
                _ = PreviousAsync();
                IsModalOpen = false;
                _ = StartModalCooldown();
                break;
        }
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

    private async Task OpenHomeItemAsync(HomeItem? item)
    {
        if (item is null) return;

        switch (item.Kind)
        {
            case HomeItemKind.Song when item.Song is not null:
                await PlaySongAsync(item.Song);
                break;
            case HomeItemKind.Album when item.Album is not null:
                await OpenAlbumAsync(item.Album);
                break;
            case HomeItemKind.Artist when item.Artist is not null:
                await OpenArtistAsync(item.Artist);
                break;
            case HomeItemKind.Playlist when item.Playlist is not null:
                await OpenPlaylistAsync(item.Playlist);
                break;
        }
    }

    private async Task LoadHomeAsync()
    {
        IsHomeLoading = true;
        HomeError = null;

        try
        {
            var response = await _homeService.GetHomeAsync();
            HomeSections.Clear();
            foreach (var section in response.Sections)
            {
                HomeSections.Add(section);
            }
        }
        catch (Exception ex)
        {
            HomeError = ex.Message;
        }
        finally
        {
            IsHomeLoading = false;
        }
    }
}