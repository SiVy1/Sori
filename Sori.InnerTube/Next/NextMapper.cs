using System.Text.Json;
using System.Text.RegularExpressions;
using Sori.Core.Models;

namespace InnerTube.Next;

public sealed class NextMapper
{
    private static readonly Regex DurationRegex =
        new(@"^\d{1,2}:\d{2}(:\d{2})?$", RegexOptions.Compiled);

    public UpNextResponse Map(Song sourceSong, JsonElement root)
    {
        var items = new List<Song>();
        string? playlistId = null;

        foreach (var renderer in FindObjects(root, "playlistPanelVideoRenderer"))
        {
            if (TryMapQueueItem(renderer, sourceSong, out var song, out var itemPlaylistId))
            {
                if (playlistId is null && !string.IsNullOrWhiteSpace(itemPlaylistId))
                {
                    playlistId = itemPlaylistId;
                }

                items.Add(song);
            }
        }

        var distinct = DistinctById(items, x => x.Id);

        var current =
            distinct.FirstOrDefault(x => x.Id == sourceSong.Id)
            ?? distinct.FirstOrDefault()
            ?? sourceSong;

        var nextItems = distinct
            .Where(x => x.Id != current.Id)
            .ToList();

        return new UpNextResponse
        {
            Current = current,
            Items = nextItems,
            PlaylistId = playlistId,
            LyricsBrowseId = FindLyricsBrowseId(root),
            RelatedBrowseId = FindRelatedBrowseId(root)
        };
    }

    private static bool TryMapQueueItem(
        JsonElement renderer,
        Song sourceSong,
        out Song song,
        out string? playlistId)
    {
        song = default!;
        playlistId = null;

        var videoId = FindVideoId(renderer);
        var title = FindFirstTextRun(renderer, "title");

        if (string.IsNullOrWhiteSpace(videoId) ||
            string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        playlistId = FindPlaylistId(renderer);

        var artist =
            FindFirstTextRun(renderer, "longBylineText")
            ?? FindFirstTextRun(renderer, "shortBylineText");

        var durationText = FindFirstTextRun(renderer, "lengthText");
        TimeSpan? duration = null;
        if (!string.IsNullOrWhiteSpace(durationText) && TryParseDuration(durationText, out var ts))
            duration = ts;

        var thumbnailUrl = FindBestThumbnailUrl(renderer);

        song = new Song
        {
            Id = "youtubeMusic:track:" + videoId,
            SourceId = videoId,
            Title = title,
            ArtistName = artist ?? "",
            AlbumTitle = null,
            Duration = duration,
            ThumbnailUrl = thumbnailUrl ?? sourceSong.ThumbnailUrl
        };

        return true;
    }

    private static string? FindVideoId(JsonElement renderer)
    {
        if (renderer.TryGetProperty("navigationEndpoint", out var navEndpoint) &&
            navEndpoint.ValueKind == JsonValueKind.Object &&
            navEndpoint.TryGetProperty("watchEndpoint", out var watchEndpoint) &&
            watchEndpoint.ValueKind == JsonValueKind.Object &&
            watchEndpoint.TryGetProperty("videoId", out var vid) &&
            vid.ValueKind == JsonValueKind.String)
        {
            return vid.GetString();
        }

        foreach (var we in FindObjects(renderer, "watchEndpoint"))
        {
            if (we.TryGetProperty("videoId", out var vid2) && vid2.ValueKind == JsonValueKind.String)
                return vid2.GetString();
        }

        return FindFirstStringProperty(renderer, "videoId");
    }

    private static string? FindPlaylistId(JsonElement renderer)
    {
        if (renderer.TryGetProperty("navigationEndpoint", out var navEndpoint) &&
            navEndpoint.ValueKind == JsonValueKind.Object &&
            navEndpoint.TryGetProperty("watchEndpoint", out var watchEndpoint) &&
            watchEndpoint.ValueKind == JsonValueKind.Object &&
            watchEndpoint.TryGetProperty("playlistId", out var pid) &&
            pid.ValueKind == JsonValueKind.String)
        {
            return pid.GetString();
        }

        foreach (var we in FindObjects(renderer, "watchEndpoint"))
        {
            if (we.TryGetProperty("playlistId", out var pid2) && pid2.ValueKind == JsonValueKind.String)
                return pid2.GetString();
        }

        return null;
    }

    private static string? FindFirstTextRun(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var runs in FindArrays(prop, "runs"))
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

    private static string? FindLyricsBrowseId(JsonElement root)
    {
        foreach (var tab in FindObjects(root, "tabRenderer"))
        {
            var title = FindFirstTextRun(tab, "title");
            if (title == "Lyrics" && tab.TryGetProperty("endpoint", out var endpoint))
            {
                foreach (var be in FindObjects(endpoint, "browseEndpoint"))
                {
                    if (be.TryGetProperty("browseId", out var bid) && bid.ValueKind == JsonValueKind.String)
                        return bid.GetString();
                }
            }
        }
        return null;
    }

    private static string? FindRelatedBrowseId(JsonElement root)
    {
        foreach (var tab in FindObjects(root, "tabRenderer"))
        {
            var title = FindFirstTextRun(tab, "title");
            if (title == "Related" && tab.TryGetProperty("endpoint", out var endpoint))
            {
                foreach (var be in FindObjects(endpoint, "browseEndpoint"))
                {
                    if (be.TryGetProperty("browseId", out var bid) && bid.ValueKind == JsonValueKind.String)
                        return bid.GetString();
                }
            }
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
