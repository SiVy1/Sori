using System.Text.Json;
using Sori.Core.Models;

namespace InnerTube.Browse;

public sealed class HomeMapper
{
    public HomeResponse MapHome(JsonElement root)
    {
        var sections = new List<HomeSection>();

        foreach (var carousel in FindObjects(root, "musicCarouselShelfRenderer"))
        {
            var title = FindCarouselTitle(carousel);
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var items = new List<HomeItem>();

            foreach (var content in FindObjects(carousel, "content"))
            {
                foreach (var twoRow in FindObjects(content, "musicTwoRowItemRenderer"))
                {
                    if (TryMapItem(twoRow, out var item))
                        items.Add(item);
                }

                foreach (var listItem in FindObjects(content, "musicResponsiveListItemRenderer"))
                {
                    if (TryMapListItem(listItem, out var item))
                        items.Add(item);
                }
            }

            // Also try direct contents array
            if (items.Count == 0 && carousel.TryGetProperty("contents", out var contentsArray)
                && contentsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var content in contentsArray.EnumerateArray())
                {
                    foreach (var twoRow in FindObjects(content, "musicTwoRowItemRenderer"))
                    {
                        if (TryMapItem(twoRow, out var item))
                            items.Add(item);
                    }

                    foreach (var listItem in FindObjects(content, "musicResponsiveListItemRenderer"))
                    {
                        if (TryMapListItem(listItem, out var item))
                            items.Add(item);
                    }
                }
            }

            if (items.Count > 0)
            {
                sections.Add(new HomeSection
                {
                    Title = title,
                    Items = items
                });
            }
        }

        return new HomeResponse
        {
            Sections = sections
        };
    }

    private static bool TryMapItem(JsonElement renderer, out HomeItem item)
    {
        item = default!;

        var pageType = FindPageType(renderer);
        var browseId = FindBrowseIdForPageType(renderer, pageType)
            ?? FindAnyBrowseId(renderer);

        var title = FindTitleText(renderer);
        var thumbnailUrl = FindBestThumbnailUrl(renderer);

        if (string.IsNullOrWhiteSpace(title))
            return false;

        // Primary: pageType
        if (pageType == "MUSIC_PAGE_TYPE_ALBUM")
        {
            item = new HomeItem
            {
                Kind = HomeItemKind.Album,
                Album = new Album
                {
                    Id = $"youtubeMusic:album:{browseId}",
                    SourceId = browseId,
                    Title = title,
                    ArtistName = FindSubtitleText(renderer) ?? "",
                    ThumbnailUrl = thumbnailUrl
                }
            };
            return true;
        }

        if (pageType == "MUSIC_PAGE_TYPE_ARTIST")
        {
            item = new HomeItem
            {
                Kind = HomeItemKind.Artist,
                Artist = new Artist
                {
                    Id = $"youtubeMusic:artist:{browseId}",
                    SourceId = browseId,
                    Name = title,
                    ThumbnailUrl = thumbnailUrl
                }
            };
            return true;
        }

        if (pageType == "MUSIC_PAGE_TYPE_PLAYLIST")
        {
            item = new HomeItem
            {
                Kind = HomeItemKind.Playlist,
                Playlist = new Playlist
                {
                    Id = $"youtubeMusic:playlist:{browseId}",
                    SourceId = browseId,
                    Title = title,
                    Description = FindSubtitleText(renderer) ?? "",
                    ThumbnailUrl = thumbnailUrl
                }
            };
            return true;
        }

        // Check for watchEndpoint -> Song
        var videoId = FindVideoId(renderer);
        if (!string.IsNullOrWhiteSpace(videoId))
        {
            item = new HomeItem
            {
                Kind = HomeItemKind.Song,
                Song = new Song
                {
                    Id = $"youtubeMusic:track:{videoId}",
                    SourceId = videoId,
                    Title = title,
                    ArtistName = FindSubtitleText(renderer) ?? "",
                    ThumbnailUrl = thumbnailUrl
                }
            };
            return true;
        }

        // Fallback: browseId prefix
        if (!string.IsNullOrWhiteSpace(browseId))
        {
            if (browseId.StartsWith("MPRE", StringComparison.OrdinalIgnoreCase))
            {
                item = new HomeItem
                {
                    Kind = HomeItemKind.Album,
                    Album = new Album
                    {
                        Id = $"youtubeMusic:album:{browseId}",
                        SourceId = browseId,
                        Title = title,
                        ArtistName = FindSubtitleText(renderer) ?? "",
                        ThumbnailUrl = thumbnailUrl
                    }
                };
                return true;
            }

            if (browseId.StartsWith("UC", StringComparison.OrdinalIgnoreCase)
                || browseId.StartsWith("MPLA", StringComparison.OrdinalIgnoreCase))
            {
                item = new HomeItem
                {
                    Kind = HomeItemKind.Artist,
                    Artist = new Artist
                    {
                        Id = $"youtubeMusic:artist:{browseId}",
                        SourceId = browseId,
                        Name = title,
                        ThumbnailUrl = thumbnailUrl
                    }
                };
                return true;
            }

            if (browseId.StartsWith("VL", StringComparison.OrdinalIgnoreCase))
            {
                item = new HomeItem
                {
                    Kind = HomeItemKind.Playlist,
                    Playlist = new Playlist
                    {
                        Id = $"youtubeMusic:playlist:{browseId}",
                        SourceId = browseId,
                        Title = title,
                        Description = FindSubtitleText(renderer) ?? "",
                        ThumbnailUrl = thumbnailUrl
                    }
                };
                return true;
            }
        }

        // Unknown renderer - ignore gracefully
        return false;
    }

    private static bool TryMapListItem(JsonElement renderer, out HomeItem item)
    {
        item = default!;

        var videoId = FindVideoId(renderer);
        var title = GetFirstFlexColumnText(renderer);

        if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(title))
            return false;

        var artist = GetFlexColumnText(renderer, 1) ?? "";
        var thumbnailUrl = FindBestThumbnailUrl(renderer);

        item = new HomeItem
        {
            Kind = HomeItemKind.Song,
            Song = new Song
            {
                Id = $"youtubeMusic:track:{videoId}",
                SourceId = videoId,
                Title = title,
                ArtistName = artist,
                ThumbnailUrl = thumbnailUrl
            }
        };

        return true;
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

    private static string? FindPageType(JsonElement renderer)
    {
        foreach (var browseEndpoint in FindObjects(renderer, "browseEndpoint"))
        {
            if (browseEndpoint.TryGetProperty("browseEndpointContextSupportedConfigs", out var configs))
            {
                foreach (var musicConfig in FindObjects(configs, "browseEndpointContextMusicConfig"))
                {
                    if (musicConfig.TryGetProperty("pageType", out var pageTypeElement)
                        && pageTypeElement.ValueKind == JsonValueKind.String)
                    {
                        return pageTypeElement.GetString();
                    }
                }
            }
        }
        return null;
    }

    private static string? FindBrowseIdForPageType(JsonElement renderer, string? targetPageType)
    {
        if (string.IsNullOrWhiteSpace(targetPageType))
            return null;

        foreach (var browseEndpoint in FindObjects(renderer, "browseEndpoint"))
        {
            if (browseEndpoint.TryGetProperty("browseEndpointContextSupportedConfigs", out var configs))
            {
                foreach (var musicConfig in FindObjects(configs, "browseEndpointContextMusicConfig"))
                {
                    if (musicConfig.TryGetProperty("pageType", out var pageTypeElement)
                        && pageTypeElement.ValueKind == JsonValueKind.String
                        && pageTypeElement.GetString() == targetPageType)
                    {
                        if (browseEndpoint.TryGetProperty("browseId", out var browseIdElement)
                            && browseIdElement.ValueKind == JsonValueKind.String)
                            return browseIdElement.GetString();
                    }
                }
            }
        }
        return null;
    }

    private static string? FindAnyBrowseId(JsonElement renderer)
    {
        foreach (var browseEndpoint in FindObjects(renderer, "browseEndpoint"))
        {
            if (browseEndpoint.TryGetProperty("browseId", out var browseIdElement)
                && browseIdElement.ValueKind == JsonValueKind.String)
            {
                return browseIdElement.GetString();
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

    private static string? FindSubtitleText(JsonElement renderer)
    {
        if (renderer.TryGetProperty("subtitle", out var subtitleElement))
        {
            var text = FindFirstTextRun(subtitleElement);
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }

    private static string? FindVideoId(JsonElement renderer)
    {
        if (renderer.TryGetProperty("playlistItemData", out var playlistItemData)
            && playlistItemData.ValueKind == JsonValueKind.Object
            && playlistItemData.TryGetProperty("videoId", out var pidVideoId)
            && pidVideoId.ValueKind == JsonValueKind.String)
        {
            return pidVideoId.GetString();
        }

        foreach (var watchEndpoint in FindObjects(renderer, "watchEndpoint"))
        {
            if (watchEndpoint.TryGetProperty("videoId", out var vid)
                && vid.ValueKind == JsonValueKind.String)
                return vid.GetString();
        }

        return FindFirstStringProperty(renderer, "videoId");
    }

    private static string? GetFirstFlexColumnText(JsonElement renderer)
    {
        return GetFlexColumnText(renderer, 0);
    }

    private static string? GetFlexColumnText(JsonElement renderer, int index)
    {
        if (!renderer.TryGetProperty("flexColumns", out var flexColumns)
            || flexColumns.ValueKind != JsonValueKind.Array
            || flexColumns.GetArrayLength() <= index)
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

    private static string? FindBestThumbnailUrl(JsonElement root)
    {
        string? bestUrl = null;
        int bestArea = 0;

        foreach (var thumbnailsArray in FindArrays(root, "thumbnails"))
        {
            foreach (var thumbnail in thumbnailsArray.EnumerateArray())
            {
                if (thumbnail.ValueKind != JsonValueKind.Object) continue;

                if (!thumbnail.TryGetProperty("url", out var urlElement)
                    || urlElement.ValueKind != JsonValueKind.String)
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

    private static string? FindFirstStringProperty(JsonElement root, string propertyName)
    {
        foreach (var value in FindStringProperties(root, propertyName))
        {
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
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
