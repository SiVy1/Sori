using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sori.Core.Models;

namespace App.ViewModels;

public sealed partial class CollectionDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "";

    [ObservableProperty]
    private string? subtitle;

    [ObservableProperty]
    private string? thumbnailUrl;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string collectionType = "";

    public ObservableCollection<Song> Tracks { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool IsEmpty => Tracks.Count == 0;

    partial void OnErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    public void LoadAlbum(Album album)
    {
        Title = album.Title;
        Subtitle = album.ArtistName;
        ThumbnailUrl = album.ThumbnailUrl;
        CollectionType = "Album";

        Tracks.Clear();
        Error = null;
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void LoadPlaylist(Playlist playlist)
    {
        Title = playlist.Title;
        Subtitle = null;
        ThumbnailUrl = playlist.ThumbnailUrl;
        CollectionType = "Playlist";

        Tracks.Clear();
        Error = null;
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void LoadDetail(CollectionDetail detail)
    {
        Title = detail.Title;
        Subtitle = detail.Subtitle;
        ThumbnailUrl = detail.ThumbnailUrl;

        Tracks.Clear();

        foreach (var track in detail.Tracks)
        {
            Tracks.Add(track);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}
