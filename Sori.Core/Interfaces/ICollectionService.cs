using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface ICollectionService
{
    Task<CollectionDetail> GetAlbumAsync(
        Album album,
        CancellationToken cancellationToken = default);

    Task<CollectionDetail> GetPlaylistAsync(
        Playlist playlist,
        CancellationToken cancellationToken = default);

    Task<ArtistDetail> GetArtistAsync(
        Artist artist,
        CancellationToken cancellationToken = default);
}
