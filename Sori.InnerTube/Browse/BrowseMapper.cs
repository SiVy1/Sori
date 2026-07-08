using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sori.Core.Models;

namespace InnerTube.Browse;

public sealed class BrowseMapper
{
    private static readonly Regex DurationRegex =
        new(@"^\d{1,2}:\d{2}(:\d{2})?$", RegexOptions.Compiled);

    public CollectionDetail MapAlbum(Album sourceAlbum, JsonElement root)
    {
        var title = FindHeaderTitle(root) ?? sourceAlbum.Title;
        var subtitle = FindHeaderSubtitle(root) ?? sourceAlbum.ArtistName;
        var thumbnailUrl = FindBestThumbnailUrl(root) ?? sourceAlbum.ThumbnailUrl;

        var tracks = new List<Song>();

        foreach (var item in FindObjects(root, "musicResponsiveListItemRenderer"))
        {
            if (TryMapAlbumTrack(item, sourceAlbum, thumbnailUrl, out var song))
            {
                tracks.Add(song);
            }
        }

        return new CollectionDetail
        {
            Id = sourceAlbum.Id,
            Title = title,
            Subtitle = subtitle,
            ThumbnailUrl = thumbnailUrl,
            Tracks = DistinctById(tracks, x => x.Id)
        };
    }

    public CollectionDetail MapPlaylist(Playlist sourcePlaylist, JsonElement root)
    {
        var title = FindPlaylistHeaderTitle(root) ?? sourcePlaylist.Title;
        var subtitle = FindPlaylistHeaderSubtitle(root);
        var thumbnailUrl = FindBestThumbnailUrl(root) ?? sourcePlaylist.ThumbnailUrl;

        var tracks = new List<Song>();

        foreach (var item in FindObjects(root, "musicResponsiveListItemRenderer"))
        {
            if (TryMapPlaylistTrack(item, sourcePlaylist, thumbnailUrl, out var song))
            {
                tracks.Add(song);
            }
        }

        return new CollectionDetail
        {
            Id = sourcePlaylist.Id,
            Title = title,
            Subtitle = subtitle,
            ThumbnailUrl = thumbnailUrl,
            Tracks = DistinctById(tracks, x => x.Id)
        };
    }

    public ArtistDetail MapArtist(Artist sourceArtist, JsonElement root)
    {
        var name = FindArtistHeaderName(root) ?? sourceArtist.Name;
        var subtitle = FindArtistHeaderSubtitle(root);
        var thumbnailUrl = FindArtistHeaderThumbnailUrl(root) ?? FindBestThumbnailUrl(root) ?? sourceArtist.ThumbnailUrl;

        var topSongs = new List<Song>();
        var albums = new List<Album>();
        var singles = new List<Album>();

        foreach (var shelf in FindObjects(root, "musicShelfRenderer"))
        {
            var shelfTitle = FindShelfTitle(shelf);

            if (IsTopSongsShelf(shelfTitle))
            {
                foreach (var item in FindObjects(shelf, "musicResponsiveListItemRenderer"))
                {
                    if (TryMapArtistTopSong(item, sourceArtist, thumbnailUrl, out var song))
                    {
                        topSongs.Add(song);
                    }
                }
            }
        }

        foreach (var carousel in FindObjects(root, "musicCarouselShelfRenderer"))
        {
            var carouselTitle = FindCarouselTitle(carousel);

            if (IsAlbumsShelf(carouselTitle))
            {
                foreach (var item in FindObjects(carousel, "musicTwoRowItemRenderer"))
                {
                    if (TryMapArtistAlbum(item, sourceArtist, out var album))
                    {
                        albums.Add(album);
                    }
                }
            }
            else if (IsSinglesShelf(carouselTitle))
            {
                foreach (var item in FindObjects(carousel, "musicTwoRowItemRenderer"))
                {
                    if (TryMapArtistAlbum(item, sourceArtist, out var single))
                    {
                        singles.Add(single);
                    }
                }
            }
        }

        return new ArtistDetail
        {
            Id = sourceArtist.Id,
            Name = name,
            Subtitle = subtitle,
            ThumbnailUrl = thumbnailUrl,
            TopSongs = DistinctById(topSongs, x => x.Id),
            Albums = DistinctById(albums, x => x.Id),
            Singles = DistinctById(singles, x => x.Id)
        };
    }

    private static bool TryMapArtistTopSong(
        JsonElement renderer,
        Artist sourceArtist,
        string? fallbackThumbnailUrl,
        out Song song)
    {
        song = default!;

        var videoId = FindVideoId(renderer);
        var title = GetFirstFlexColumnText(renderer);

        if (string.IsNullOrWhiteSpace(videoId) ||
            string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var subtitleParts = GetUsefulTextsFromFlexColumn(renderer, 1);
        var artistName = subtitleParts.ElementAtOrDefault(0) ?? sourceArtist.Name;
        var albumTitle = subtitleParts.ElementAtOrDefault(1);
        var duration = FindDuration(renderer);
        var thumbnailUrl = FindBestThumbnailUrl(renderer) ?? fallbackThumbnailUrl;

        song = new Song
        {
            Id = $"youtubeMusic:track:{videoId}",
            Title = title,
            ArtistName = artistName,
            AlbumTitle = albumTitle,
            Duration = duration,
            ThumbnailUrl = thumbnailUrl
        };

        return true;
    }

    private static bool TryMapArtistAlbum(
        JsonElement renderer,
        Artist sourceArtist,
        out Album album)
    {
        album = default!;

        var browseId = FindBrowseIdForPageType(renderer, "MUSIC_PAGE_TYPE_ALBUM")
            ?? FindFirstBrowseIdWithPrefix(renderer, "MPRE");

        var title = FindTitleText(renderer);
        var thumbnailUrl = FindBestThumbnailUrl(renderer);

        if (string.IsNullOrWhiteSpace(browseId) ||
            string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        album = new Album
        {
            Id = $"youtubeMusic:album:{browseId}",
            SourceId = browseId,
            Title = title,
            ArtistName = sourceArtist.Name,
            ThumbnailUrl = thumbnailUrl
        };

        return true;
    }

    private static string? FindArtistHeaderName(JsonElement root)
    {
        foreach (var headerType in new[] { "musicImmersiveHeaderRenderer", "musicVisualHeaderRenderer", "musicDetailHeaderRenderer" })
        {
            foreach (var header in FindObjects(root, headerType))
            {
                if (header.TryGetProperty("title", out var titleElement))
                {
                    var text = FindFirstTextRun(titleElement);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        return null;
    }

    private static string? FindArtistHeaderThumbnailUrl(JsonElement root)
    {
        foreach (var headerType in new[] { "musicImmersiveHeaderRenderer", "musicVisualHeaderRenderer", "musicDetailHeaderRenderer" })
        {
            foreach (var header in FindObjects(root, headerType))
            {
                if (header.TryGetProperty("thumbnail", out var thumbContainer))
                {
                    var url = FindBestThumbnailUrl(thumbContainer);
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }
            }
        }
        return null;
    }

    private static string? FindArtistHeaderSubtitle(JsonElement root)
    {
        foreach (var headerType in new[] { "musicImmersiveHeaderRenderer", "musicVisualHeaderRenderer", "musicDetailHeaderRenderer" })
        {
            foreach (var header in FindObjects(root, headerType))
            {
                if (header.TryGetProperty("description", out var descElement))
                {
                    var text = FindFirstTextRun(descElement);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
                if (header.TryGetProperty("subtitle", out var subElement))
                {
                    var text = FindFirstTextRun(subElement);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        return null;
    }

    private static string? FindShelfTitle(JsonElement shelf)
    {
        if (shelf.TryGetProperty("title", out var titleElement))
        {
            var text = FindFirstTextRun(titleElement);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }

    private static string? FindCarouselTitle(JsonElement carousel)
    {
        if (carousel.TryGetProperty("header", out var headerElement))
        {
            foreach (var basicHeader in FindObjects(headerElement, "musicCarouselShelfBasicHeaderRenderer"))
            {
                if (basicHeader.TryGetProperty("title", out var titleElement))
                {
                    var text = FindFirstTextRun(titleElement);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        return null;
    }

    private static string? FindTitleText(JsonElement renderer)
    {
        if (renderer.TryGetProperty("title", out var titleElement))
        {
            var text = FindFirstTextRun(titleElement);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }

    private static string? FindBrowseIdForPageType(JsonElement root, string targetPageType)
    {
        foreach (var browseEndpoint in FindObjects(root, "browseEndpoint"))
        {
            if (browseEndpoint.TryGetProperty("browseEndpointContextSupportedConfigs", out var configs))
            {
                foreach (var musicConfig in FindObjects(configs, "browseEndpointContextMusicConfig"))
                {
                    if (musicConfig.TryGetProperty("pageType", out var pageTypeElement) &&
                        pageTypeElement.ValueKind == JsonValueKind.String &&
                        pageTypeElement.GetString() == targetPageType)
                    {
                        if (browseEndpoint.TryGetProperty("browseId", out var browseIdElement) && browseIdElement.ValueKind == JsonValueKind.String)
                            return browseIdElement.GetString();
                    }
                }
            }
        }
        return null;
    }

    private static string? FindFirstBrowseIdWithPrefix(JsonElement root, string prefix)
    {
        foreach (var value in FindStringProperties(root, "browseId"))
        {
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value;
        }
        return null;
    }

    private static bool IsTopSongsShelf(string? title)
    {
        return string.Equals(title, "Top songs", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(title, "Songs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlbumsShelf(string? title)
    {
        return string.Equals(title, "Albums", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSinglesShelf(string? title)
    {
        return string.Equals(title, "Singles", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(title, "Singles & EPs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryMapPlaylistTrack(
        JsonElement renderer,
        Playlist sourcePlaylist,
        string? fallbackThumbnailUrl,
        out Song song)
    {
        song = default!;

        var videoId = FindVideoId(renderer);
        var title = GetFirstFlexColumnText(renderer);

        if (string.IsNullOrWhiteSpace(videoId) ||
            string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var subtitleParts = GetUsefulTextsFromFlexColumn(renderer, 1);
        var artist = subtitleParts.ElementAtOrDefault(0) ?? "";
        var album = subtitleParts.ElementAtOrDefault(1);
        var duration = FindDuration(renderer);
        var thumbnail = FindBestThumbnailUrl(renderer) ?? fallbackThumbnailUrl;

        song = new Song
        {
            Id = $"youtubeMusic:track:{videoId}",
            Title = title,
            ArtistName = artist,
            AlbumTitle = album,
            Duration = duration,
            ThumbnailUrl = thumbnail
        };

        return true;
    }

    private static IReadOnlyList<string> GetUsefulTextsFromFlexColumn(JsonElement renderer, int index)
    {
        var result = new List<string>();

        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() <= index)
            return result;

        var column = flexColumns[index];
        if (!column.TryGetProperty("musicResponsiveListItemFlexColumnRenderer", out var colRenderer))
            return result;

        foreach (var runsArray in FindArrays(colRenderer, "runs"))
        {
            foreach (var run in runsArray.EnumerateArray())
            {
                if (run.ValueKind == JsonValueKind.Object && run.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text) && text != " • ")
                        result.Add(text);
                }
            }
        }

        return result;
    }

    private static string? FindPlaylistHeaderTitle(JsonElement root)
    {
        // Try musicEditablePlaylistDetailHeaderRenderer -> header -> musicDetailHeaderRenderer -> title
        foreach (var editableHeader in FindObjects(root, "musicEditablePlaylistDetailHeaderRenderer"))
        {
            if (editableHeader.TryGetProperty("header", out var header))
            {
                foreach (var detailHeader in FindObjects(header, "musicDetailHeaderRenderer"))
                {
                    if (detailHeader.TryGetProperty("title", out var titleElement))
                    {
                        var text = FindFirstTextRun(titleElement);
                        if (!string.IsNullOrWhiteSpace(text))
                            return text;
                    }
                }
            }
        }

        // Fallback: direct musicDetailHeaderRenderer
        return FindHeaderTitle(root);
    }

    private static string? FindPlaylistHeaderSubtitle(JsonElement root)
    {
        // Try musicEditablePlaylistDetailHeaderRenderer -> header -> musicDetailHeaderRenderer -> subtitle
        foreach (var editableHeader in FindObjects(root, "musicEditablePlaylistDetailHeaderRenderer"))
        {
            if (editableHeader.TryGetProperty("header", out var header))
            {
                foreach (var detailHeader in FindObjects(header, "musicDetailHeaderRenderer"))
                {
                    if (detailHeader.TryGetProperty("subtitle", out var subtitleElement))
                    {
                        var texts = GetTextRuns(subtitleElement);
                        var filtered = texts
                            .Where(t => t != "Playlist" && t != " • ")
                            .ToList();

                        if (filtered.Count > 0)
                            return string.Join(" • ", filtered);
                    }
                }
            }
        }

        // Fallback: direct musicDetailHeaderRenderer subtitle
        return FindHeaderSubtitle(root);
    }

    private static bool TryMapAlbumTrack(
        JsonElement renderer,
        Album sourceAlbum,
        string? albumThumbnailUrl,
        out Song song)
    {
        song = default!;

        var videoId = FindVideoId(renderer);
        var title = GetFirstFlexColumnText(renderer);

        if (string.IsNullOrWhiteSpace(videoId) ||
            string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var artist = GetFlexColumnText(renderer, 1) ?? sourceAlbum.ArtistName;
        var duration = FindDuration(renderer);

        song = new Song
        {
            Id = $"youtubeMusic:track:{videoId}",
            Title = title,
            ArtistName = artist,
            AlbumTitle = sourceAlbum.Title,
            Duration = duration,
            ThumbnailUrl = albumThumbnailUrl
        };

        return true;
    }

    private static string? FindHeaderTitle(JsonElement root)
    {
        foreach (var header in FindObjects(root, "musicDetailHeaderRenderer"))
        {
            if (header.TryGetProperty("title", out var titleElement))
            {
                var text = FindFirstTextRun(titleElement);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }
        return null;
    }

    private static string? FindHeaderSubtitle(JsonElement root)
    {
        foreach (var header in FindObjects(root, "musicDetailHeaderRenderer"))
        {
            if (header.TryGetProperty("subtitle", out var subtitleElement))
            {
                var texts = GetTextRuns(subtitleElement);
                var filtered = texts
                    .Where(t => t != "Album" && t != " • ")
                    .ToList();

                if (filtered.Count > 0)
                    return string.Join(" • ", filtered);
            }
        }
        return null;
    }

    private static string? GetFirstFlexColumnText(JsonElement renderer)
    {
        return GetFlexColumnText(renderer, 0);
    }

    private static string? GetFlexColumnText(JsonElement renderer, int index)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() <= index)
            return null;

        var column = flexColumns[index];
        if (!column.TryGetProperty("musicResponsiveListItemFlexColumnRenderer", out var colRenderer))
            return null;

        return FindFirstTextRun(colRenderer);
    }

    private static string? FindFirstTextRun(JsonElement root)
    {
        foreach (var runs in FindArrays(root, "runs"))
        {
            foreach (var run in runs.EnumerateArray())
            {
                if (run.ValueKind == JsonValueKind.Object && run.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text) && text != " • ")
                        return text;
                }
            }
        }
        return null;
    }

    private static IReadOnlyList<string> GetTextRuns(JsonElement root)
    {
        var result = new List<string>();
        foreach (var runsArray in FindArrays(root, "runs"))
        {
            foreach (var run in runsArray.EnumerateArray())
            {
                if (run.ValueKind == JsonValueKind.Object && run.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text) && text != " • ")
                        result.Add(text);
                }
            }
        }
        return result;
    }

    private static string? FindVideoId(JsonElement renderer)
    {
        if (renderer.TryGetProperty("playlistItemData", out var playlistItemData) &&
            playlistItemData.ValueKind == JsonValueKind.Object &&
            playlistItemData.TryGetProperty("videoId", out var pidVideoId) &&
            pidVideoId.ValueKind == JsonValueKind.String)
        {
            return pidVideoId.GetString();
        }

        foreach (var watchEndpoint in FindObjects(renderer, "watchEndpoint"))
        {
            if (watchEndpoint.TryGetProperty("videoId", out var vid) && vid.ValueKind == JsonValueKind.String)
                return vid.GetString();
        }

        return FindFirstStringProperty(renderer, "videoId");
    }

    private static TimeSpan? FindDuration(JsonElement renderer)
    {
        // Try fixedColumns first
        if (renderer.TryGetProperty("fixedColumns", out var fixedColumns) &&
            fixedColumns.ValueKind == JsonValueKind.Array)
        {
            foreach (var column in fixedColumns.EnumerateArray())
            {
                if (column.TryGetProperty("musicResponsiveListItemFixedColumnRenderer", out var fixedCol))
                {
                    var text = FindFirstTextRun(fixedCol);
                    if (!string.IsNullOrWhiteSpace(text) && DurationRegex.IsMatch(text))
                    {
                        if (TryParseDuration(text, out var duration))
                            return duration;
                    }
                }
            }
        }

        // Fallback: any duration-like text
        foreach (var text in GetTextRuns(renderer))
        {
            if (DurationRegex.IsMatch(text))
            {
                if (TryParseDuration(text, out var duration))
                    return duration;
            }
        }

        return null;
    }

    private static bool TryParseDuration(string text, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        var parts = text.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds))
        {
            duration = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            return true;
        }
        if (parts.Length == 3 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var mins) && int.TryParse(parts[2], out var secs))
        {
            duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(mins) + TimeSpan.FromSeconds(secs);
            return true;
        }
        return false;
    }

    private static string? FindBestThumbnailUrl(JsonElement root)
    {
        string? bestUrl = null;
        int bestArea = 0;

        foreach (var thumbnailsArray in FindArrays(root, "thumbnails"))
        {
            foreach (var thumbnail in thumbnailsArray.EnumerateArray())
            {
                if (thumbnail.ValueKind != JsonValueKind.Object) continue;

                if (!thumbnail.TryGetProperty("url", out var urlElement) || urlElement.ValueKind != JsonValueKind.String)
                    continue;

                var url = urlElement.GetString();
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    continue;

                int width = 0, height = 0;
                if (thumbnail.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number)
                    width = w.GetInt32();
                if (thumbnail.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number)
                    height = h.GetInt32();

                int area = width * height;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestUrl = url;
                }
            }
        }

        return bestUrl;
    }

    private static IReadOnlyList<T> DistinctById<T>(IEnumerable<T> items, Func<T, string?> keySelector)
    {
        var seen = new HashSet<string>();
        var result = new List<T>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (seen.Add(key)) result.Add(item);
        }
        return result;
    }

    private static IEnumerable<JsonElement> FindObjects(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Object)
                    yield return property.Value;
                foreach (var child in FindObjects(property.Value, propertyName)) yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                foreach (var child in FindObjects(item, propertyName)) yield return child;
        }
    }

    private static IEnumerable<JsonElement> FindArrays(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Array)
                    yield return property.Value;
                foreach (var child in FindArrays(property.Value, propertyName)) yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                foreach (var child in FindArrays(item, propertyName)) yield return child;
        }
    }

    private static IEnumerable<string> FindStringProperties(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (value is not null) yield return value;
                }
                foreach (var child in FindStringProperties(property.Value, propertyName)) yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                foreach (var child in FindStringProperties(item, propertyName)) yield return child;
        }
    }

    private static string? FindFirstStringProperty(JsonElement root, string propertyName)
    {
        foreach (var value in FindStringProperties(root, propertyName))
        {
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }
}
