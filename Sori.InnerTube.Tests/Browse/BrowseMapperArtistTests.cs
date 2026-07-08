using System.Text.Json;
using InnerTube.Browse;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Browse;

public class BrowseMapperArtistTests
{
    private readonly BrowseMapper _mapper = new();

    [Fact]
    public void MapArtist_WithMinimalArtistBrowseJson_ReturnsTopSongs()
    {
        var json = @"
{
    ""header"": {
        ""musicImmersiveHeaderRenderer"": {
            ""title"": { ""runs"": [{ ""text"": ""Test Artist"" }] },
            ""description"": { ""runs"": [{ ""text"": ""1.2M subscribers"" }] },
            ""thumbnail"": {
                ""musicThumbnailRenderer"": {
                    ""thumbnail"": {
                        ""thumbnails"": [
                            { ""url"": ""https://example.com/thumb-60.jpg"", ""width"": 60, ""height"": 60 },
                            { ""url"": ""https://example.com/thumb-120.jpg"", ""width"": 120, ""height"": 120 }
                        ]
                    }
                }
            }
        }
    },
    ""contents"": {
        ""singleColumnBrowseResultsRenderer"": {
            ""tabs"": [{
                ""tabRenderer"": {
                    ""content"": {
                        ""sectionListRenderer"": {
                            ""contents"": [
                                {
                                    ""musicShelfRenderer"": {
                                        ""title"": { ""runs"": [{ ""text"": ""Top songs"" }] },
                                        ""contents"": [
                                            {
                                                ""musicResponsiveListItemRenderer"": {
                                                    ""flexColumns"": [
                                                        {
                                                            ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""Top Song 1"" }] }
                                                            }
                                                        },
                                                        {
                                                            ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""Test Artist"" }, { ""text"": "" • "" }, { ""text"": ""Album 1"" }] }
                                                            }
                                                        }
                                                    ],
                                                    ""fixedColumns"": [
                                                        {
                                                            ""musicResponsiveListItemFixedColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""3:23"" }] }
                                                            }
                                                        }
                                                    ],
                                                    ""playlistItemData"": { ""videoId"": ""topsong1"" }
                                                }
                                            },
                                            {
                                                ""musicResponsiveListItemRenderer"": {
                                                    ""flexColumns"": [
                                                        {
                                                            ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""Top Song 2"" }] }
                                                            }
                                                        },
                                                        {
                                                            ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""Test Artist"" }] }
                                                            }
                                                        }
                                                    ],
                                                    ""fixedColumns"": [
                                                        {
                                                            ""musicResponsiveListItemFixedColumnRenderer"": {
                                                                ""text"": { ""runs"": [{ ""text"": ""4:05"" }] }
                                                            }
                                                        }
                                                    ],
                                                    ""playlistItemData"": { ""videoId"": ""topsong2"" }
                                                }
                                            }
                                        ]
                                    }
                                },
                                {
                                    ""musicCarouselShelfRenderer"": {
                                        ""header"": {
                                            ""musicCarouselShelfBasicHeaderRenderer"": {
                                                ""title"": { ""runs"": [{ ""text"": ""Albums"" }] }
                                            }
                                        },
                                        ""contents"": [
                                            {
                                                ""musicTwoRowItemRenderer"": {
                                                    ""title"": {
                                                        ""runs"": [
                                                            {
                                                                ""text"": ""Album 1"",
                                                                ""navigationEndpoint"": {
                                                                    ""browseEndpoint"": {
                                                                        ""browseId"": ""MPRE_album1"",
                                                                        ""browseEndpointContextSupportedConfigs"": {
                                                                            ""browseEndpointContextMusicConfig"": {
                                                                                ""pageType"": ""MUSIC_PAGE_TYPE_ALBUM""
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        ]
                                                    },
                                                    ""thumbnailRenderer"": {
                                                        ""musicThumbnailRenderer"": {
                                                            ""thumbnail"": {
                                                                ""thumbnails"": [
                                                                    { ""url"": ""https://example.com/album-thumb.jpg"", ""width"": 226, ""height"": 226 }
                                                                ]
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                }
            }]
        }
    }
}";
        using var doc = JsonDocument.Parse(json);
        var sourceArtist = new Artist
        {
            Id = "youtubeMusic:artist:UCtest",
            SourceId = "UCtest",
            Name = "Fallback Name"
        };

        var detail = _mapper.MapArtist(sourceArtist, doc.RootElement);

        Assert.Equal("Test Artist", detail.Name);
        Assert.Equal("1.2M subscribers", detail.Subtitle);
        Assert.Equal("https://example.com/thumb-120.jpg", detail.ThumbnailUrl);
        Assert.Equal(2, detail.TopSongs.Count);
        Assert.Single(detail.Albums);
        Assert.Empty(detail.Singles);

        var song1 = detail.TopSongs[0];
        Assert.Equal("youtubeMusic:track:topsong1", song1.Id);
        Assert.Equal("Top Song 1", song1.Title);
        Assert.Equal("Test Artist", song1.ArtistName);
        Assert.Equal("Album 1", song1.AlbumTitle);
        Assert.Equal(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(23), song1.Duration);

        var album1 = detail.Albums[0];
        Assert.Equal("youtubeMusic:album:MPRE_album1", album1.Id);
        Assert.Equal("MPRE_album1", album1.SourceId);
        Assert.Equal("Album 1", album1.Title);
        Assert.Equal("Test Artist", album1.ArtistName);
    }

    [Fact]
    public void Debug_FindObjectsInBigJson()
    {
        var json = @"
{
    ""header"": {
        ""musicImmersiveHeaderRenderer"": {
            ""title"": { ""runs"": [{ ""text"": ""Test Artist"" }] },
            ""description"": { ""runs"": [{ ""text"": ""1.2M subscribers"" }] },
            ""thumbnail"": {
                ""musicThumbnailRenderer"": {
                    ""thumbnail"": {
                        ""thumbnails"": [
                            { ""url"": ""https://example.com/thumb-60.jpg"", ""width"": 60, ""height"": 60 },
                            { ""url"": ""https://example.com/thumb-120.jpg"", ""width"": 120, ""height"": 120 }
                        ]
                    }
                }
            }
        }
    },
    ""contents"": {
        ""singleColumnBrowseResultsRenderer"": {
            ""tabs"": [{
                ""tabRenderer"": {
                    ""content"": {
                        ""sectionListRenderer"": {
                            ""contents"": [
                                {
                                    ""musicShelfRenderer"": {
                                        ""title"": { ""runs"": [{ ""text"": ""Top songs"" }] },
                                        ""contents"": [{
                                            ""musicResponsiveListItemRenderer"": {
                                                ""flexColumns"": [
                                                    { ""musicResponsiveListItemFlexColumnRenderer"": { ""text"": { ""runs"": [{ ""text"": ""Song"" }] } } }
                                                ],
                                                ""playlistItemData"": { ""videoId"": ""test"" }
                                            }
                                        }]
                                    }
                                }
                            ]
                        }
                    }
                }
            }]
        }
    }
}";
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var found = new List<JsonElement>();
        FindObjectsTest(root, "musicImmersiveHeaderRenderer", found);
        Assert.Single(found);
        var header = found[0];
        Assert.True(header.TryGetProperty("title", out var titleEl));
        Assert.Equal("Test Artist", titleEl.GetProperty("runs")[0].GetProperty("text").GetString());

        // Now test via MapArtist
        var artist = new Artist { Id = "test", SourceId = "UCtest", Name = "Fallback" };
        var detail = _mapper.MapArtist(artist, doc.RootElement);
        Assert.Equal("Test Artist", detail.Name);
    }

    [Fact]
    public void Debug_FindObjectsDirectly()
    {
        // Same JSON as the main test but only header
        var json = @"
{
    ""header"": {
        ""musicImmersiveHeaderRenderer"": {
            ""title"": { ""runs"": [{ ""text"": ""Test Artist"" }] }
        }
    },
    ""contents"": {}
}";
        using var doc = JsonDocument.Parse(json);
        // Test FindObjects directly using the pattern from BrowseMapper
        var root = doc.RootElement;
        var found = new List<JsonElement>();
        FindObjectsTest(root, "musicImmersiveHeaderRenderer", found);
        Assert.Single(found);
        var header = found[0];
        Assert.True(header.TryGetProperty("title", out var titleEl));
        Assert.Equal("Test Artist", titleEl.GetProperty("runs")[0].GetProperty("text").GetString());
    }

    private static void FindObjectsTest(JsonElement element, string propertyName, List<JsonElement> results)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Object)
                    results.Add(property.Value);
                FindObjectsTest(property.Value, propertyName, results);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                FindObjectsTest(item, propertyName, results);
        }
    }

    [Fact]
    public void MapArtist_UsesSourceArtistFallbacks()
    {
        var json = "{ \"contents\": { \"singleColumnBrowseResultsRenderer\": { \"tabs\": [] } } }";
        using var doc = JsonDocument.Parse(json);

        var sourceArtist = new Artist
        {
            Id = "youtubeMusic:artist:UCtest",
            SourceId = "UCtest",
            Name = "Source Name",
            ThumbnailUrl = "https://example.com/source.jpg"
        };

        var detail = _mapper.MapArtist(sourceArtist, doc.RootElement);

        Assert.Equal("Source Name", detail.Name);
        Assert.Null(detail.Subtitle);
        Assert.Equal("https://example.com/source.jpg", detail.ThumbnailUrl);
        Assert.Empty(detail.TopSongs);
        Assert.Empty(detail.Albums);
        Assert.Empty(detail.Singles);
    }

    [Fact]
    public void MapArtist_DoesNotDuplicateTopSongs()
    {
        var json = @"
{
    ""contents"": {
        ""singleColumnBrowseResultsRenderer"": {
            ""tabs"": [{
                ""tabRenderer"": {
                    ""content"": {
                        ""sectionListRenderer"": {
                            ""contents"": [{
                                ""musicShelfRenderer"": {
                                    ""title"": { ""runs"": [{ ""text"": ""Top songs"" }] },
                                    ""contents"": [
                                        {
                                            ""musicResponsiveListItemRenderer"": {
                                                ""flexColumns"": [
                                                    {
                                                        ""musicResponsiveListItemFlexColumnRenderer"": {
                                                            ""text"": { ""runs"": [{ ""text"": ""Song 1"" }] }
                                                        }
                                                    }
                                                ],
                                                ""playlistItemData"": { ""videoId"": ""sameId"" }
                                            }
                                        },
                                        {
                                            ""musicResponsiveListItemRenderer"": {
                                                ""flexColumns"": [
                                                    {
                                                        ""musicResponsiveListItemFlexColumnRenderer"": {
                                                            ""text"": { ""runs"": [{ ""text"": ""Song 1 Dup"" }] }
                                                        }
                                                    }
                                                ],
                                                ""playlistItemData"": { ""videoId"": ""sameId"" }
                                            }
                                        }
                                    ]
                                }
                            }]
                        }
                    }
                }
            }]
        }
    }
}";
        using var doc = JsonDocument.Parse(json);
        var sourceArtist = new Artist
        {
            Id = "youtubeMusic:artist:UCtest",
            SourceId = "UCtest",
            Name = "Artist"
        };

        var detail = _mapper.MapArtist(sourceArtist, doc.RootElement);

        Assert.Single(detail.TopSongs);
    }
}
