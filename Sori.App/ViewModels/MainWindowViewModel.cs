using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly IPlaybackCoordinator _playbackCoordinator;
    private readonly IQueueService _queueService;
    private readonly IPrefetchingPlaybackResolver? _prefetchResolver;
    private readonly ICollectionService _collectionService;
    private readonly IUpNextService _upNextService;

    [ObservableProperty] private MainContentView currentView = MainContentView.Home;

    [ObservableProperty] private bool isModalOpen;

    [ObservableProperty] private bool canOpenModal = true;

    [ObservableProperty] private bool isQueueVisible = true;

    [ObservableProperty] private Song? currentSong;

    public MainWindowViewModel(
        ISearchService searchService,
        IQueueService queueService,
        ICollectionService collectionService,
        IHomeService homeService,
        IUpNextService upNextService,
        IPlaybackCoordinator playbackCoordinator,
        IPrefetchingPlaybackResolver? prefetchResolver = null)
    {
        _playbackCoordinator = playbackCoordinator;
        _queueService = queueService;
        _prefetchResolver = prefetchResolver;
        _collectionService = collectionService;
        _upNextService = upNextService;

        Player = new PlayerBarViewModel(playbackCoordinator, queueService);
        Queue = new QueueViewModel(queueService);
        Search = new SearchViewModel(searchService);
        Home = new HomeViewModel(homeService);

        WireSearchEvents();

        PlaySongCommand = new AsyncRelayCommand<Song>(song => PlaySongAsync(song, null));
        OpenAlbumCommand = new AsyncRelayCommand<Album>(OpenAlbumAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist>(OpenPlaylistAsync);
        OpenArtistCommand = new AsyncRelayCommand<Artist>(OpenArtistAsync);
        OpenHomeItemCommand = new AsyncRelayCommand<HomeItem>(OpenHomeItemAsync);
        BackToSearchCommand = new RelayCommand(BackToSearch);
        ToggleModalCommand = new RelayCommand(ToggleModal);
        ToggleQueueCommand = new RelayCommand(ToggleQueue);
        PlayCollectionCommand = new AsyncRelayCommand(PlayCollectionAsync);
        PlayCollectionTrackCommand = new AsyncRelayCommand<Song>(PlayCollectionTrackAsync);
        AddNextCommand = new RelayCommand<Song>(AddNext);
        AddToQueueEndCommand = new RelayCommand<Song>(AddToQueueEnd);
    }

    public PlayerBarViewModel Player { get; }
    public QueueViewModel Queue { get; }
    public SearchViewModel Search { get; }
    public HomeViewModel Home { get; }

    public CollectionDetailViewModel CollectionDetail { get; } = new();
    public ArtistDetailViewModel ArtistDetail { get; } = new();

    public bool IsHomeView => CurrentView == MainContentView.Home;
    public bool IsCollectionDetailView => CurrentView == MainContentView.CollectionDetail;
    public bool IsArtistDetailView => CurrentView == MainContentView.ArtistDetail;

    public IAsyncRelayCommand<Song> PlaySongCommand { get; }
    public IAsyncRelayCommand<Album> OpenAlbumCommand { get; }
    public IAsyncRelayCommand<Playlist> OpenPlaylistCommand { get; }
    public IAsyncRelayCommand<Artist> OpenArtistCommand { get; }
    public IAsyncRelayCommand<HomeItem> OpenHomeItemCommand { get; }
    public IRelayCommand BackToSearchCommand { get; }
    public IRelayCommand ToggleModalCommand { get; }
    public IRelayCommand ToggleQueueCommand { get; }
    public IAsyncRelayCommand PlayCollectionCommand { get; }
    public IAsyncRelayCommand<Song> PlayCollectionTrackCommand { get; }
    public IRelayCommand<Song> AddNextCommand { get; }
    public IRelayCommand<Song> AddToQueueEndCommand { get; }

    partial void OnCurrentViewChanged(MainContentView value)
    {
        OnPropertyChanged(nameof(IsHomeView));
        OnPropertyChanged(nameof(IsCollectionDetailView));
        OnPropertyChanged(nameof(IsArtistDetailView));
    }

    private void WireSearchEvents()
    {
        // ponytail: keep modal open for queue/playback actions; close only for navigation
        Search.SongActivated += async (_, args) =>
        {
            await PlaySongAsync(args.Song, args.ContextSongs);
        };

        Search.AlbumActivated += async (_, album) =>
        {
            IsModalOpen = false;
            _ = StartModalCooldown();
            await OpenAlbumAsync(album);
        };

        Search.ArtistActivated += async (_, artist) =>
        {
            IsModalOpen = false;
            _ = StartModalCooldown();
            await OpenArtistAsync(artist);
        };

        Search.PlaylistActivated += async (_, playlist) =>
        {
            IsModalOpen = false;
            _ = StartModalCooldown();
            await OpenPlaylistAsync(playlist);
        };

        Search.HomeRequested += (_, _) =>
        {
            CurrentView = MainContentView.Home;
            IsModalOpen = false;
            _ = StartModalCooldown();
        };

        Search.CloseRequested += (_, _) =>
        {
            IsModalOpen = false;
            _ = StartModalCooldown();
        };

        Search.ToggleQueueRequested += (_, _) =>
        {
            ToggleQueue();
        };

        Search.PlaySelectedRequested += async (_, _) =>
        {
            var selectedIndex = Search.ModalSelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < Search.ModalItems.Count)
            {
                var item = Search.ModalItems[selectedIndex];
                if (item.Kind == HomeItemKind.Song && item.Song is not null)
                {
                    await PlaySongAsync(item.Song, null);
                }
            }
        };

        Search.TogglePlayPauseRequested += async (_, _) =>
        {
            await Player.TogglePlayPauseCommand.ExecuteAsync(null);
        };

        Search.NextRequested += async (_, _) =>
        {
            await Player.NextCommand.ExecuteAsync(null);
        };

        Search.PreviousRequested += async (_, _) =>
        {
            await Player.PreviousCommand.ExecuteAsync(null);
        };

        Search.AddToQueueRequested += (_, song) =>
        {
            AddToQueueEnd(song);
        };

        Search.PlayNowRequested += async (_, song) =>
        {
            await PlaySongAsync(song, null);
        };
    }

    private async Task PlaySongAsync(Song? song, IEnumerable<Song>? contextSongs = null)
    {
        if (song is null) return;

        _prefetchResolver?.Clear();

        if (contextSongs is not null)
        {
            _queueService.SetContext(contextSongs, song);
            CurrentSong = song;
            await PlayCurrentQueueItemAsync();
            return;
        }

        var wasEmpty = _queueService.Items.Count == 0;
        if (wasEmpty)
        {
            Search.IsAddingToQueue = true;
            // First song: fetch /next radio queue
            try
            {
                var upNext = await _upNextService.GetUpNextAsync(song);
                _queueService.SetQueue([upNext.Current], 0);
                if (upNext.Items.Count > 0)
                {
                    _queueService.SetRadioQueue(upNext.Items);
                }
            }
            catch (Exception)
            {
                _queueService.SetQueue([song], 0);
            }
            finally
            {
                Search.IsAddingToQueue = false;
            }
            CurrentSong = song;
            await PlayCurrentQueueItemAsync();
        }
        else
        {
            // ponytail: queue not empty — add to end of user queue
            _queueService.AddToTheEnd(song);
        }
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
            Player.PlaybackError = null;
            Player.IsPlaybackLoading = true;
            await _playbackCoordinator.PlayQueueItemAsync();
        }
        catch (Exception ex)
        {
            Player.PlaybackError = ex.Message;
            if (CurrentSong == current) CurrentSong = null;
        }
        finally
        {
            Player.IsPlaybackLoading = false;
        }
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

    private void ToggleQueue()
    {
        IsQueueVisible = !IsQueueVisible;
    }

    private void AddNext(Song? song)
    {
        if (song is null) return;

        var wasEmpty = _queueService.Items.Count == 0;
        _queueService.AddNext(song);

        if (wasEmpty && ShouldAutoPlay())
        {
            _ = PlayCurrentQueueItemAsync();
        }
    }

    private void AddToQueueEnd(Song? song)
    {
        if (song is null) return;

        var wasEmpty = _queueService.Items.Count == 0;
        _queueService.AddToTheEnd(song);

        if (wasEmpty && ShouldAutoPlay())
        {
            _ = PlayCurrentQueueItemAsync();
        }
    }

    private bool ShouldAutoPlay()
    {
        var state = _playbackCoordinator.Snapshot.State;
        return state is not PlaybackState.Playing and not PlaybackState.Paused and not PlaybackState.Loading;
    }

    public async Task StartModalCooldown()
    {
        CanOpenModal = false;
        await Task.Delay(800);
        CanOpenModal = true;
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
}
