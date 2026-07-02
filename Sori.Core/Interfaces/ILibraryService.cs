using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface ILibraryService
{
    Task<IReadOnlyList<Playlist>> GetPlaylistsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Album>> GetAlbumsAsync(
        CancellationToken cancellationToken = default);
}
