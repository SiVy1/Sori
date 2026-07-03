namespace Sori.Core.Models;

public sealed class Playlist
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? ThumbnailUrl { get; init; }
    public string Description { get; init; } = "";
    public int SongCount { get; init; }
}