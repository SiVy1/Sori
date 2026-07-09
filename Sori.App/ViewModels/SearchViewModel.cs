using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using App.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;

    [ObservableProperty] private string? searchError;

    [ObservableProperty] private string searchQuery = "";

    [ObservableProperty] private string commandQuery = "";

    [ObservableProperty] private bool isCommandMode;

    [ObservableProperty] private SearchState searchState = SearchState.Idle;

    [ObservableProperty] private int modalSelectedIndex = -1;

    public SearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        ExecuteCommandCommand = new RelayCommand(ExecuteCommand);

        SearchSongs = new ObservableCollection<Song>();
        SearchAlbums = new ObservableCollection<Album>();
        SearchArtists = new ObservableCollection<Artist>();
        SearchPlaylists = new ObservableCollection<Playlist>();
        ModalItems = new ObservableCollection<HomeItem>();
    }

    public ObservableCollection<Song> SearchSongs { get; }
    public ObservableCollection<Album> SearchAlbums { get; }
    public ObservableCollection<Artist> SearchArtists { get; }
    public ObservableCollection<Playlist> SearchPlaylists { get; }
    public ObservableCollection<HomeItem> ModalItems { get; }
    public ObservableCollection<CommandItem> CommandItems { get; } = new();

    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand ExecuteCommandCommand { get; }

    public event EventHandler<SongActivationEventArgs>? SongActivated;
    public event EventHandler<Album>? AlbumActivated;
    public event EventHandler<Artist>? ArtistActivated;
    public event EventHandler<Playlist>? PlaylistActivated;
    public event EventHandler? HomeRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? ToggleQueueRequested;
    public event EventHandler? TogglePlayPauseRequested;
    public event EventHandler? PlaySelectedRequested;
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler<Song>? AddToQueueRequested;
    public event EventHandler<Song>? PlayNowRequested;

    private static readonly List<CommandItem> AllCommands = new()
    {
        new("home", "Go to home", "Ctrl+Space > home"),
        new("search", "Search music", "Ctrl+Space > search"),
        new("queue", "Toggle queue visibility", "Ctrl+Space > queue"),
        new("play", "Play selected track", "Ctrl+Space > play"),
        new("pause", "Pause playback", "Ctrl+Space > pause"),
        new("next", "Next track", "Ctrl+Space > next"),
        new("prev", "Previous track", "Ctrl+Space > prev")
    };

    partial void OnSearchQueryChanged(string value)
    {
        IsCommandMode = value.StartsWith(">");
        CommandQuery = IsCommandMode ? value[1..].TrimStart() : "";

        if (IsCommandMode)
        {
            FilterCommands();
        }
    }

    private void FilterCommands()
    {
        CommandItems.Clear();

        var query = CommandQuery.Trim().ToLowerInvariant();

        var filtered = string.IsNullOrEmpty(query)
            ? AllCommands
            : AllCommands.Where(c => c.Name.Contains(query) || c.Description.ToLowerInvariant().Contains(query));

        foreach (var cmd in filtered)
        {
            CommandItems.Add(cmd);
        }

        ModalSelectedIndex = CommandItems.Count > 0 ? 0 : -1;
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

    public void ActivateSelectedItem()
    {
        if (ModalSelectedIndex < 0 || ModalSelectedIndex >= ModalItems.Count)
        {
            return;
        }

        ActivateItem(ModalItems[ModalSelectedIndex]);
    }

    public void ActivateItem(HomeItem item)
    {
        switch (item.Kind)
        {
            case HomeItemKind.Song when item.Song is not null:
                SongActivated?.Invoke(this, new SongActivationEventArgs(item.Song, SearchSongs.ToList()));
                break;
            case HomeItemKind.Album when item.Album is not null:
                AlbumActivated?.Invoke(this, item.Album);
                break;
            case HomeItemKind.Artist when item.Artist is not null:
                ArtistActivated?.Invoke(this, item.Artist);
                break;
            case HomeItemKind.Playlist when item.Playlist is not null:
                PlaylistActivated?.Invoke(this, item.Playlist);
                break;
        }
    }

    public void AddSelectedToQueue()
    {
        if (ModalSelectedIndex < 0 || ModalSelectedIndex >= ModalItems.Count)
            return;

        var item = ModalItems[ModalSelectedIndex];
        if (item.Kind == HomeItemKind.Song && item.Song is not null)
        {
            AddToQueueRequested?.Invoke(this, item.Song);
        }
        else
        {
            // For non-song items, fall back to opening them
            ActivateItem(item);
        }
    }

    public void PlaySelectedNow()
    {
        if (ModalSelectedIndex < 0 || ModalSelectedIndex >= ModalItems.Count)
            return;

        var item = ModalItems[ModalSelectedIndex];
        if (item.Kind == HomeItemKind.Song && item.Song is not null)
        {
            PlayNowRequested?.Invoke(this, item.Song);
        }
        else
        {
            ActivateItem(item);
        }
    }

    private void ExecuteCommand()
    {
        if (ModalSelectedIndex >= 0 && ModalSelectedIndex < CommandItems.Count)
        {
            ExecuteCommandByName(CommandItems[ModalSelectedIndex].Name);
            return;
        }

        var cmd = CommandQuery.Trim().ToLowerInvariant();
        ExecuteCommandByName(cmd);
    }

    private void ExecuteCommandByName(string cmd)
    {
        switch (cmd)
        {
            case "home":
                HomeRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "search":
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "queue":
                ToggleQueueRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "play":
                PlaySelectedRequested?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "pause":
                TogglePlayPauseRequested?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "next":
                NextRequested?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "prev":
            case "previous":
                PreviousRequested?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
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

    public bool HasSongs => SearchSongs.Count > 0;
    public bool HasAlbums => SearchAlbums.Count > 0;
    public bool HasArtists => SearchArtists.Count > 0;
    public bool HasPlaylists => SearchPlaylists.Count > 0;
}

public sealed class CommandItem
{
    public string Name { get; }
    public string Description { get; }
    public string Shortcut { get; }

    public CommandItem(string name, string description, string shortcut = "")
    {
        Name = name;
        Description = description;
        Shortcut = shortcut;
    }
}

public sealed class SongActivationEventArgs : EventArgs
{
    public Song Song { get; }
    public IReadOnlyList<Song> ContextSongs { get; }

    public SongActivationEventArgs(Song song, IReadOnlyList<Song> contextSongs)
    {
        Song = song;
        ContextSongs = contextSongs;
    }
}
