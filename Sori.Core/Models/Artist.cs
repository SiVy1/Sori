namespace Sori.Core.Models;

public sealed class Artist
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ThumbnailUrl { get; init; }
}