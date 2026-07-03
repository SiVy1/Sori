using System.Text.Json;
using Sori.Core.Models;

namespace InnerTube.Search;

public sealed class SearchMapper
{
    public SearchResponse Map(string query, JsonElement root)
    {
        var songs = new List<Song>();

        foreach (var item in FindObjects(root, "musicResponsiveListItemRenderer"))
            if (TryMapSong(item, out var song))
                songs.Add(song);

        return new SearchResponse
        {
            Songs = songs
        };
    }

    private static bool TryMapSong(JsonElement renderer, out Song song)
    {
        song = default!;

        var videoId = FindFirstStringProperty(renderer, "videoId");
        var title = TryGetTitle(renderer);

        if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(title)) return false;

        var subtitle = TryGetSubtitle(renderer);
        var thumbnailUrl = TryGetThumbnailUrl(renderer);

        song = new Song
        {
            Id = $"youtubeMusic:track:{videoId}",
            Title = title,
            ArtistName = subtitle,
            AlbumTitle = null,
            Duration = null,
            ThumbnailUrl = thumbnailUrl
        };

        return true;
    }

    private static string? TryGetTitle(JsonElement renderer)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() == 0)
            return null;

        var firstColumn = flexColumns[0];

        return FindFirstTextRun(firstColumn);
    }

    private static string? TryGetSubtitle(JsonElement renderer)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns) ||
            flexColumns.ValueKind != JsonValueKind.Array ||
            flexColumns.GetArrayLength() < 2)
            return null;

        var secondColumn = flexColumns[1];

        return FindFirstTextRun(secondColumn);
    }

    private static string? TryGetThumbnailUrl(JsonElement renderer)
    {
        string? best = null;

        foreach (var url in FindStringProperties(renderer, "url"))
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                best = url;

        return best;
    }

    private static string? FindFirstTextRun(JsonElement root)
    {
        foreach (var runs in FindArrays(root, "runs"))
        foreach (var run in runs.EnumerateArray())
            if (run.ValueKind == JsonValueKind.Object &&
                run.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();

                if (!string.IsNullOrWhiteSpace(text) && text != " • ") return text;
            }

        return null;
    }

    private static string? FindFirstStringProperty(JsonElement root, string propertyName)
    {
        foreach (var value in FindStringProperties(root, propertyName))
            if (!string.IsNullOrWhiteSpace(value))
                return value;

        return null;
    }

    private static IEnumerable<string> FindStringProperties(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();

                    if (value is not null) yield return value;
                }

                foreach (var child in FindStringProperties(property.Value, propertyName)) yield return child;
            }
        else if (root.ValueKind == JsonValueKind.Array)
            foreach (var item in root.EnumerateArray())
            foreach (var child in FindStringProperties(item, propertyName))
                yield return child;
    }

    private static IEnumerable<JsonElement> FindObjects(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) &&
                    property.Value.ValueKind == JsonValueKind.Object)
                    yield return property.Value;

                foreach (var child in FindObjects(property.Value, propertyName)) yield return child;
            }
        else if (root.ValueKind == JsonValueKind.Array)
            foreach (var item in root.EnumerateArray())
            foreach (var child in FindObjects(item, propertyName))
                yield return child;
    }

    private static IEnumerable<JsonElement> FindArrays(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) &&
                    property.Value.ValueKind == JsonValueKind.Array)
                    yield return property.Value;

                foreach (var child in FindArrays(property.Value, propertyName)) yield return child;
            }
        else if (root.ValueKind == JsonValueKind.Array)
            foreach (var item in root.EnumerateArray())
            foreach (var child in FindArrays(item, propertyName))
                yield return child;
    }
}