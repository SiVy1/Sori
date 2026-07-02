using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public sealed class SearchResponse
{
    public IReadOnlyList<Song> Songs { get; init; } = [];
    public IReadOnlyList<Album> Albums { get; init; } = [];
    public IReadOnlyList<Artist> Artists { get; init; } = [];
    public IReadOnlyList<Playlist> Playlists { get; init; } = [];
}
