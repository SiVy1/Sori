namespace Sori.Core.Models;

public enum HomeItemKind
{
    Song,
    Album,
    Artist,
    Playlist
}

public sealed class HomeItem
{
    public HomeItemKind Kind { get; init; }
    public Song? Song { get; init; }
    public Album? Album { get; init; }
    public Artist? Artist { get; init; }
    public Playlist? Playlist { get; init; }

    // ponytail: flattened display properties so XAML doesn't need nested fallback bindings
    public string? ThumbnailUrl => Song?.ThumbnailUrl ?? Album?.ThumbnailUrl ?? Artist?.ThumbnailUrl ?? Playlist?.ThumbnailUrl;
    public string? Title => Song?.Title ?? Album?.Title ?? Artist?.Name ?? Playlist?.Title;
    public string? Subtitle => Song?.ArtistName ?? Album?.ArtistName ?? Artist?.Name ?? Playlist?.Description;
}

public sealed class HomeSection
{
    public string Title { get; init; } = "";
    public IReadOnlyList<HomeItem> Items { get; init; } = [];
}

public sealed class HomeResponse
{
    public IReadOnlyList<HomeSection> Sections { get; init; } = [];
}
