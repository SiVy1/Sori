using Sori.Core.Models;

public sealed class SearchResponse
{
    public IReadOnlyList<Song> Songs { get; init; } = [];
    public IReadOnlyList<Album> Albums { get; init; } = [];
    public IReadOnlyList<Artist> Artists { get; init; } = [];
    public IReadOnlyList<Playlist> Playlists { get; init; } = [];
    
    public bool IsEmpty => Songs.Count == 0 && Albums.Count == 0 && Artists.Count == 0 && Playlists.Count == 0;
}
