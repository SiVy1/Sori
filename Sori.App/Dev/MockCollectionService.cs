using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Dev;

public sealed class MockCollectionService : ICollectionService
{
    public Task<CollectionDetail> GetAlbumAsync(
        Album album,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CollectionDetail
        {
            Id = album.Id,
            Title = album.Title,
            Subtitle = album.ArtistName,
            ThumbnailUrl = album.ThumbnailUrl,
            Tracks =
            [
                new Song
                {
                    Id = "mock:track:1",
                    Title = "Mock Track 1",
                    ArtistName = album.ArtistName,
                    AlbumTitle = album.Title,
                    ThumbnailUrl = album.ThumbnailUrl
                }
            ]
        });
    }

    public Task<CollectionDetail> GetPlaylistAsync(
        Playlist playlist,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CollectionDetail
        {
            Id = playlist.Id,
            Title = playlist.Title,
            Subtitle = "Mock playlist",
            ThumbnailUrl = playlist.ThumbnailUrl,
            Tracks =
            [
                new Song
                {
                    Id = "mock:playlist-track:1",
                    Title = "Mock Playlist Track 1",
                    ArtistName = "Mock Artist",
                    AlbumTitle = playlist.Title,
                    ThumbnailUrl = playlist.ThumbnailUrl
                }
            ]
        });
    }

    public Task<ArtistDetail> GetArtistAsync(
        Artist artist,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ArtistDetail
        {
            Id = artist.Id,
            Name = artist.Name,
            Subtitle = "Mock artist",
            ThumbnailUrl = artist.ThumbnailUrl,
            TopSongs =
            [
                new Song
                {
                    Id = "mock:artist-track:1",
                    Title = "Mock Top Song",
                    ArtistName = artist.Name,
                    AlbumTitle = "Mock Album",
                    ThumbnailUrl = artist.ThumbnailUrl
                }
            ],
            Albums =
            [
                new Album
                {
                    Id = "mock:artist-album:1",
                    SourceId = "MPRE_mock",
                    Title = "Mock Album",
                    ArtistName = artist.Name,
                    ThumbnailUrl = artist.ThumbnailUrl
                }
            ]
        });
    }
}
