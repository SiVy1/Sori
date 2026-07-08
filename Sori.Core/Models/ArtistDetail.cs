namespace Sori.Core.Models;

public sealed class ArtistDetail
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Subtitle { get; init; }
    public string? ThumbnailUrl { get; init; }

    public IReadOnlyList<Song> TopSongs { get; init; } = [];
    public IReadOnlyList<Album> Albums { get; init; } = [];
    public IReadOnlyList<Album> Singles { get; init; } = [];
}
