using System.Text.Json;
using System.Text.RegularExpressions;
using Sori.Core.Models;

namespace InnerTube.Search;

public sealed class SearchMapper
{
    private static readonly HashSet<string> IgnoredTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Song", "Album", "Artist", "Playlist", "Video", "Community playlist", " • ", ""
    };

    public SearchResponse Map(string query, JsonElement root)
    {
        var songs = new List<Song>();
        var albums = new List<Album>();
        var artists = new List<Artist>();
        var playlists = new List<Playlist>();

        foreach (var item in FindObjects(root, "musicResponsiveListItemRenderer"))
        {
            // Classification order: Album -> Artist -> Playlist -> Song
            if (TryMapAlbum(item, out var album))
            {
                albums.Add(album);
                continue;
            }

            if (TryMapArtist(item, out var artist))
            {
                artists.Add(artist);
                continue;
            }

            if (TryMapPlaylist(item, out var playlist))
            {
                playlists.Add(playlist);
                continue;
            }

            if (TryMapSong(item, out var song))
            {
                songs.Add(song);
            }
        }

        return new SearchResponse
        {
            Songs = DistinctById(songs, x => x.Id),
            Albums = DistinctById(albums, x => x.Id),
            Artists = DistinctById(artists, x => x.Id),
            Playlists = DistinctById(playlists, x => x.Id)
        };
    }

    // --- Classification & Mapping ---

    private static bool TryMapAlbum(JsonElement renderer, out Album album)
    {
        album = default!;

        var pageType = TryGetTopLevelPageType(renderer);
        var browseId = TryGetTopLevelBrowseId(renderer);
        var title = GetTitle(renderer);

        // Primary: top-level pageType match
        if (pageType == "MUSIC_PAGE_TYPE_ALBUM" &&
            !string.IsNullOrWhiteSpace(browseId) &&
            !string.IsNullOrWhiteSpace(title))
        {
            album = CreateAlbum(browseId, title, renderer);
            return true;
        }

        // Fallback: subtitle indicates Album/Single + browseId prefix
        var subtitleTexts = GetSubtitleRuns(renderer);
        var firstSubtitle = subtitleTexts.FirstOrDefault();
        if ((firstSubtitle == "Album" || firstSubtitle == "Single") &&
            !string.IsNullOrWhiteSpace(browseId) &&
            browseId.StartsWith("MPRE", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(title))
        {
            album = CreateAlbum(browseId, title, renderer);
            return true;
        }

        return false;
    }

    private static bool TryMapArtist(JsonElement renderer, out Artist artist)
    {
        artist = default!;

        var pageType = TryGetTopLevelPageType(renderer);
        var browseId = TryGetTopLevelBrowseId(renderer);
        var title = GetTitle(renderer);

        if (pageType == "MUSIC_PAGE_TYPE_ARTIST" &&
            !string.IsNullOrWhiteSpace(browseId) &&
            !string.IsNullOrWhiteSpace(title))
        {
            artist = new Artist
            {
                Id = $"youtubeMusic:artist:{browseId}",
                SourceId = browseId,
                Name = title,
                ThumbnailUrl = FindBestThumbnailUrl(renderer)
            };
            return true;
        }

        // Fallback: subtitle indicates Artist + browseId prefix
        var subtitleTexts = GetSubtitleRuns(renderer);
        var firstSubtitle = subtitleTexts.FirstOrDefault();
        if (firstSubtitle == "Artist" &&
            !string.IsNullOrWhiteSpace(browseId) &&
            browseId.StartsWith("UC", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(title))
        {
            artist = new Artist
            {
                Id = $"youtubeMusic:artist:{browseId}",
                SourceId = browseId,
                Name = title,
                ThumbnailUrl = FindBestThumbnailUrl(renderer)
            };
            return true;
        }

        return false;
    }

    private static bool TryMapPlaylist(JsonElement renderer, out Playlist playlist)
    {
        playlist = default!;

        var pageType = TryGetTopLevelPageType(renderer);
        var browseId = TryGetTopLevelBrowseId(renderer);
        var title = GetTitle(renderer);

        var sourceId = FindFirstStringProperty(renderer, "playlistId");

        if (string.IsNullOrWhiteSpace(sourceId) &&
            !string.IsNullOrWhiteSpace(browseId) &&
            browseId.StartsWith("VL", StringComparison.OrdinalIgnoreCase))
        {
            sourceId = browseId[2..];
        }

        if (pageType == "MUSIC_PAGE_TYPE_PLAYLIST" &&
            !string.IsNullOrWhiteSpace(sourceId) &&
            !string.IsNullOrWhiteSpace(title))
        {
            playlist = new Playlist
            {
                Id = $"youtubeMusic:playlist:{sourceId}",
                SourceId = sourceId,
                Title = title,
                ThumbnailUrl = FindBestThumbnailUrl(renderer)
            };
            return true;
        }

        // Fallback: subtitle indicates Playlist + sourceId
        var subtitleTexts = GetSubtitleRuns(renderer);
        var firstSubtitle = subtitleTexts.FirstOrDefault();
        if (firstSubtitle == "Playlist" &&
            !string.IsNullOrWhiteSpace(sourceId) &&
            !string.IsNullOrWhiteSpace(title))
        {
            playlist = new Playlist
            {
                Id = $"youtubeMusic:playlist:{sourceId}",
                SourceId = sourceId,
                Title = title,
                ThumbnailUrl = FindBestThumbnailUrl(renderer)
            };
            return true;
        }

        return false;
    }

    private static bool TryMapSong(JsonElement renderer, out Song song)
    {
        song = default!;

        var videoId = FindVideoId(renderer);
        var title = GetTitle(renderer);

        if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(title))
            return false;

        var texts = GetSubtitleRuns(renderer);
        var artistName = ExtractArtistName(texts);
        var albumTitle = ExtractAlbumTitle(texts);
        var durationText = FindDuration(texts);
        TimeSpan? duration = null;
        if (!string.IsNullOrWhiteSpace(durationText) && TryParseDuration(durationText, out var ts))
            duration = ts;

        song = new Song
        {
            Id = $"youtubeMusic:track:{videoId}",
            SourceId = videoId,
            Title = title,
            ArtistName = artistName ?? "",
            AlbumTitle = albumTitle,
            Duration = duration,
            ThumbnailUrl = FindBestThumbnailUrl(renderer)
        };

        return true;
    }

    // --- Top-Level PageType & BrowseId Helpers ---

    private static string? TryGetTopLevelPageType(JsonElement renderer)
    {
        // Primary: top-level navigationEndpoint -> browseEndpoint -> pageType
        if (renderer.TryGetProperty("navigationEndpoint", out var navEndpoint) &&
            navEndpoint.ValueKind == JsonValueKind.Object)
        {
            if (navEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
                browseEndpoint.ValueKind == JsonValueKind.Object)
            {
                var pageType = ExtractPageTypeFromBrowseEndpoint(browseEndpoint);
                if (!string.IsNullOrWhiteSpace(pageType))
                    return pageType;
            }
        }

        // Fallback: first flexColumn -> runs[0] -> navigationEndpoint -> browseEndpoint -> pageType
        if (renderer.TryGetProperty("flexColumns", out var flexColumns) &&
            flexColumns.ValueKind == JsonValueKind.Array &&
            flexColumns.GetArrayLength() > 0)
        {
            var firstColumn = flexColumns[0];
            if (firstColumn.TryGetProperty("musicResponsiveListItemFlexColumnRenderer", out var colRenderer))
            {
                var pageType = FindPageTypeInTextRuns(colRenderer);
                if (!string.IsNullOrWhiteSpace(pageType))
                    return pageType;
            }
        }

        return null;
    }

    private static string? TryGetTopLevelBrowseId(JsonElement renderer)
    {
        // Primary: top-level navigationEndpoint -> browseEndpoint -> browseId
        if (renderer.TryGetProperty("navigationEndpoint", out var navEndpoint) &&
            navEndpoint.ValueKind == JsonValueKind.Object)
        {
            if (navEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
                browseEndpoint.ValueKind == JsonValueKind.Object &&
                browseEndpoint.TryGetProperty("browseId", out var browseIdElement) &&
                browseIdElement.ValueKind == JsonValueKind.String)
            {
                return browseIdElement.GetString();
            }
        }

        // Fallback: first flexColumn -> runs[0] -> navigationEndpoint -> browseEndpoint -> browseId
        if (renderer.TryGetProperty("flexColumns", out var flexColumns) &&
            flexColumns.ValueKind == JsonValueKind.Array &&
            flexColumns.GetArrayLength() > 0)
        {
            var firstColumn = flexColumns[0];
            if (firstColumn.TryGetProperty("musicResponsiveListItemFlexColumnRenderer", out var colRenderer))
            {
                var browseId = FindBrowseIdInTextRuns(colRenderer);
                if (!string.IsNullOrWhiteSpace(browseId))
                    return browseId;
            }
        }

        return null;
    }

    private static string? ExtractPageTypeFromBrowseEndpoint(JsonElement browseEndpoint)
    {
        if (browseEndpoint.TryGetProperty("browseEndpointContextSupportedConfigs", out var configs) &&
            configs.ValueKind == JsonValueKind.Object)
        {
            if (configs.TryGetProperty("browseEndpointContextMusicConfig", out var musicConfig) &&
                musicConfig.ValueKind == JsonValueKind.Object &&
                musicConfig.TryGetProperty("pageType", out var pageTypeElement) &&
                pageTypeElement.ValueKind == JsonValueKind.String)
            {
                return pageTypeElement.GetString();
            }
        }
        return null;
    }

    private static string? FindPageTypeInTextRuns(JsonElement columnRenderer)
    {
        if (!columnRenderer.TryGetProperty("text", out var textElement) ||
            textElement.ValueKind != JsonValueKind.Object)
            return null;

        if (!textElement.TryGetProperty("runs", out var runs) ||
            runs.ValueKind != JsonValueKind.Array ||
            runs.GetArrayLength() == 0)
            return null;

        var firstRun = runs[0];
        if (firstRun.ValueKind != JsonValueKind.Object)
            return null;

        if (!firstRun.TryGetProperty("navigationEndpoint", out var navEndpoint) ||
            navEndpoint.ValueKind != JsonValueKind.Object)
            return null;

        if (!navEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) ||
            browseEndpoint.ValueKind != JsonValueKind.Object)
            return null;

        return ExtractPageTypeFromBrowseEndpoint(browseEndpoint);
    }

    private static string? FindBrowseIdInTextRuns(JsonElement columnRenderer)
    {
        if (!columnRenderer.TryGetProperty("text", out var textElement) ||
            textElement.ValueKind != JsonValueKind.Object)
            return null;

        if (!textElement.TryGetProperty("runs", out var runs) ||
            runs.ValueKind != JsonValueKind.Array ||
            runs.GetArrayLength() == 0)
            return null;

        var firstRun = runs[0];
        if (firstRun.ValueKind != JsonValueKind.Object)
            return null;

        if (!firstRun.TryGetProperty("navigationEndpoint", out var navEndpoint) ||
            navEndpoint.ValueKind != JsonValueKind.Object)
            return null;

        if (!navEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) ||
            browseEndpoint.ValueKind != JsonValueKind.Object)
            return null;

        if (browseEndpoint.TryGetProperty("browseId", out var browseIdElement) &&
            browseIdElement.ValueKind == JsonValueKind.String)
        {
            return browseIdElement.GetString();
        }

        return null;
    }

    // --- VideoId Helpers ---

    private static string? FindVideoId(JsonElement renderer)
    {
        // Priority 1: playlistItemData.videoId (top-level in renderer)
        if (renderer.TryGetProperty("playlistItemData", out var playlistItemData) &&
            playlistItemData.ValueKind == JsonValueKind.Object &&
            playlistItemData.TryGetProperty("videoId", out var pidVideoId) &&
            pidVideoId.ValueKind == JsonValueKind.String)
        {
            return pidVideoId.GetString();
        }

        // Priority 2: watchEndpoint.videoId in top-level navigationEndpoint
        if (renderer.TryGetProperty("navigationEndpoint", out var navEndpoint) &&
            navEndpoint.ValueKind == JsonValueKind.Object &&
            navEndpoint.TryGetProperty("watchEndpoint", out var watchEndpoint) &&
            watchEndpoint.ValueKind == JsonValueKind.Object &&
            watchEndpoint.TryGetProperty("videoId", out var navVideoId) &&
            navVideoId.ValueKind == JsonValueKind.String)
        {
            return navVideoId.GetString();
        }

        // Priority 3: watchEndpoint.videoId in first flexColumn
        if (renderer.TryGetProperty("flexColumns", out var flexColumns) &&
            flexColumns.ValueKind == JsonValueKind.Array &&
            flexColumns.GetArrayLength() > 0)
        {
            var firstColumn = flexColumns[0];
            if (firstColumn.TryGetProperty("musicResponsiveListItemFlexColumnRenderer", out var colRenderer) &&
                colRenderer.ValueKind == JsonValueKind.Object &&
                colRenderer.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.Object &&
                textElement.TryGetProperty("runs", out var runs) &&
                runs.ValueKind == JsonValueKind.Array &&
                runs.GetArrayLength() > 0)
            {
                var firstRun = runs[0];
                if (firstRun.ValueKind == JsonValueKind.Object &&
                    firstRun.TryGetProperty("navigationEndpoint", out var runNavEndpoint) &&
                    runNavEndpoint.ValueKind == JsonValueKind.Object &&
                    runNavEndpoint.TryGetProperty("watchEndpoint", out var runWatchEndpoint) &&
                    runWatchEndpoint.ValueKind == JsonValueKind.Object &&
                    runWatchEndpoint.TryGetProperty("videoId", out var runVideoId) &&
                    runVideoId.ValueKind == JsonValueKind.String)
                {
                    return runVideoId.GetString();
                }
            }
        }

        // Priority 4: any videoId anywhere (fallback)
        return FindFirstStringProperty(renderer, "videoId");
    }

    // --- Existing Helpers (unchanged) ---

    private static Album CreateAlbum(string browseId, string title, JsonElement renderer)
    {
        var texts = GetSubtitleRuns(renderer);
        var artistName = ExtractArtistName(texts);
        var year = texts.FirstOrDefault(t => Regex.IsMatch(t, @"^\d{4}$"));

        return new Album
        {
            Id = $"youtubeMusic:album:{browseId}",
            SourceId = browseId,
            Title = title,
            ArtistName = artistName ?? "",
            Year = year ?? "",
            ThumbnailUrl = FindBestThumbnailUrl(renderer)
        };
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
                    if (!string.IsNullOrWhiteSpace(text) && !IgnoredTexts.Contains(text))
                        result.Add(text);
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<string> GetSubtitleRuns(JsonElement renderer)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() < 2)
            return Array.Empty<string>();

        var secondColumn = flexColumns[1];
        var result = new List<string>();

        foreach (var runsArray in FindArrays(secondColumn, "runs"))
        {
            foreach (var run in runsArray.EnumerateArray())
            {
                if (run.ValueKind == JsonValueKind.Object && run.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text) && !IgnoredTexts.Contains(text))
                        result.Add(text);
                }
            }
        }

        return result;
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

    private static string? FindDuration(IReadOnlyList<string> texts)
    {
        foreach (var text in texts)
        {
            if (Regex.IsMatch(text, @"^\d{1,2}:\d{2}(:\d{2})?$"))
                return text;
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

    private static string? GetTitle(JsonElement renderer)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() == 0)
            return null;

        var firstColumn = flexColumns[0];
        return FindFirstTextRun(firstColumn);
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

    private static string? ExtractArtistName(IReadOnlyList<string> texts)
    {
        // After removing ignored texts, artist is typically the first meaningful text after title
        // In practice, from the second flexColumn runs, the first non-ignored non-duration text is the artist
        foreach (var text in texts)
        {
            if (Regex.IsMatch(text, @"^\d{1,2}:\d{2}(:\d{2})?$")) continue; // skip duration
            if (Regex.IsMatch(text, @"^\d{4}$")) continue; // skip year
            return text;
        }
        return null;
    }

    private static string? ExtractAlbumTitle(IReadOnlyList<string> texts)
    {
        // Album title is typically after artist name
        bool foundArtist = false;
        foreach (var text in texts)
        {
            if (Regex.IsMatch(text, @"^\d{1,2}:\d{2}(:\d{2})?$")) continue;
            if (Regex.IsMatch(text, @"^\d{4}$")) continue;
            if (!foundArtist)
            {
                foundArtist = true;
                continue;
            }
            return text;
        }
        return null;
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

    // --- Recursive JSON traversal ---

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
}
