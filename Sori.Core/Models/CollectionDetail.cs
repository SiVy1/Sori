namespace Sori.Core.Models;

public sealed class CollectionDetail
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public string? ThumbnailUrl { get; init; }

    public IReadOnlyList<Song> Tracks { get; init; } = [];
}
