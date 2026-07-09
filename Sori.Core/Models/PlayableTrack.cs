namespace Sori.Core.Models;

public sealed class PlayableTrack
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";

    public string? ArtistName { get; init; }
    public string? AlbumTitle { get; init; }
    public string? ThumbnailUrl { get; init; }

    public string Source { get; init; } = "";
    public string SourceId { get; init; } = "";

    public Uri? StreamUri { get; init; }

    public bool HasStreamUri => StreamUri is not null;
}
