namespace Sori.Core.Models;

public sealed class UpNextResponse
{
    public Song Current { get; init; } = default!;
    public IReadOnlyList<Song> Items { get; init; } = [];
    public string? LyricsBrowseId { get; init; }
    public string? RelatedBrowseId { get; init; }
    public string? PlaylistId { get; init; }
}
