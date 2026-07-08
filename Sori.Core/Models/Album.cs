namespace Sori.Core.Models;

public sealed class Album
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string ArtistName { get; init; } = "";
    public string Year { get; init; } = "";
    public string? ThumbnailUrl { get; init; }
    public string? SourceId { get; init; }
}