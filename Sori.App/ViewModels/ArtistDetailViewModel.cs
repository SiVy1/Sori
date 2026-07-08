using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sori.Core.Models;

namespace App.ViewModels;

public sealed partial class ArtistDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string? subtitle;

    [ObservableProperty]
    private string? thumbnailUrl;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? error;

    public ObservableCollection<Song> TopSongs { get; } = new();
    public ObservableCollection<Album> Albums { get; } = new();
    public ObservableCollection<Album> Singles { get; } = new();

    public bool HasTopSongs => TopSongs.Count > 0;
    public bool HasAlbums => Albums.Count > 0;
    public bool HasSingles => Singles.Count > 0;

    public void LoadArtist(Artist artist)
    {
        Name = artist.Name;
        Subtitle = null;
        ThumbnailUrl = artist.ThumbnailUrl;

        TopSongs.Clear();
        Albums.Clear();
        Singles.Clear();

        NotifySectionsChanged();
    }

    public void LoadDetail(ArtistDetail detail)
    {
        Name = detail.Name;
        Subtitle = detail.Subtitle;
        ThumbnailUrl = detail.ThumbnailUrl;

        TopSongs.Clear();
        Albums.Clear();
        Singles.Clear();

        foreach (var song in detail.TopSongs)
        {
            TopSongs.Add(song);
        }

        foreach (var album in detail.Albums)
        {
            Albums.Add(album);
        }

        foreach (var single in detail.Singles)
        {
            Singles.Add(single);
        }

        NotifySectionsChanged();
    }

    private void NotifySectionsChanged()
    {
        OnPropertyChanged(nameof(HasTopSongs));
        OnPropertyChanged(nameof(HasAlbums));
        OnPropertyChanged(nameof(HasSingles));
    }
}
