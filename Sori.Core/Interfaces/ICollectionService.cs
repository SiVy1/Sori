using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface ICollectionService
{
    Task<Playlist?> GetPlaylistAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<Album?> GetAlbumAsync(
        string id,
        CancellationToken cancellationToken = default);
}