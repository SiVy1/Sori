using System.Text.Json;
using InnerTube.Browse;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Browse;

public class BrowseMapperTests
{
    private readonly BrowseMapper _mapper = new();

    [Fact]
    public void MapAlbum_WithMinimalAlbumBrowseJson_ReturnsTracks()
    {
        var json = @"
        {
            ""header"": {
                ""musicDetailHeaderRenderer"": {
                    ""title"": { ""runs"": [{ ""text"": ""Test Album"" }] },
                    ""subtitle"": { ""runs"": [{ ""text"": ""Album"" }, { ""text"": "" • "" }, { ""text"": ""Test Artist"" }] },
                    ""thumbnail"": {
                        ""croppedSquareThumbnailRenderer"": {
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
                ""twoColumnBrowseResultsRenderer"": {
                    ""tabs"": [{
                        ""tabRenderer"": {
                            ""content"": {
                                ""sectionListRenderer"": {
                                    ""contents"": [{
                                        ""musicShelfRenderer"": {
                                            ""contents"": [
                                                {
                                                    ""musicResponsiveListItemRenderer"": {
                                                        ""flexColumns"": [
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Track 1"" }] }
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
                                                                    ""text"": { ""runs"": [{ ""text"": ""3:23"" }] }
                                                                }
                                                            }
                                                        ],
                                                        ""playlistItemData"": { ""videoId"": ""abc123"" }
                                                    }
                                                },
                                                {
                                                    ""musicResponsiveListItemRenderer"": {
                                                        ""flexColumns"": [
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Track 2"" }] }
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
                                                        ""playlistItemData"": { ""videoId"": ""def456"" }
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
        var sourceAlbum = new Album
        {
            Id = "youtubeMusic:album:MPRE_test",
            SourceId = "MPRE_test",
            Title = "Fallback Title",
            ArtistName = "Fallback Artist"
        };

        var detail = _mapper.MapAlbum(sourceAlbum, doc.RootElement);

        Assert.Equal("Test Album", detail.Title);
        Assert.Equal("Test Artist", detail.Subtitle);
        Assert.Equal("https://example.com/thumb-120.jpg", detail.ThumbnailUrl);
        Assert.Equal(2, detail.Tracks.Count);

        var track1 = detail.Tracks[0];
        Assert.Equal("youtubeMusic:track:abc123", track1.Id);
        Assert.Equal("Track 1", track1.Title);
        Assert.Equal("Test Artist", track1.ArtistName);
        Assert.Equal(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(23), track1.Duration);

        var track2 = detail.Tracks[1];
        Assert.Equal("youtubeMusic:track:def456", track2.Id);
        Assert.Equal("Track 2", track2.Title);
        Assert.Equal(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(5), track2.Duration);
    }

    [Fact]
    public void MapAlbum_WithMinimalAlbumBrowseJson_UsesSourceAlbumFallbacks()
    {
        var json = "{ \"contents\": { \"twoColumnBrowseResultsRenderer\": { \"tabs\": [] } } }";
        using var doc = JsonDocument.Parse(json);

        var sourceAlbum = new Album
        {
            Id = "youtubeMusic:album:MPRE_test",
            SourceId = "MPRE_test",
            Title = "Source Title",
            ArtistName = "Source Artist",
            ThumbnailUrl = "https://example.com/source.jpg"
        };

        var detail = _mapper.MapAlbum(sourceAlbum, doc.RootElement);

        Assert.Equal("Source Title", detail.Title);
        Assert.Equal("Source Artist", detail.Subtitle);
        Assert.Equal("https://example.com/source.jpg", detail.ThumbnailUrl);
        Assert.Empty(detail.Tracks);
    }

    [Fact]
    public void MapAlbum_DoesNotReturnDuplicateTracks()
    {
        var json = @"
        {
            ""contents"": {
                ""twoColumnBrowseResultsRenderer"": {
                    ""tabs"": [{
                        ""tabRenderer"": {
                            ""content"": {
                                ""sectionListRenderer"": {
                                    ""contents"": [{
                                        ""musicShelfRenderer"": {
                                            ""contents"": [
                                                {
                                                    ""musicResponsiveListItemRenderer"": {
                                                        ""flexColumns"": [
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Track 1"" }] }
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
                                                                    ""text"": { ""runs"": [{ ""text"": ""Track 1 Dup"" }] }
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
        var sourceAlbum = new Album
        {
            Id = "youtubeMusic:album:MPRE_test",
            SourceId = "MPRE_test",
            Title = "Album",
            ArtistName = "Artist"
        };

        var detail = _mapper.MapAlbum(sourceAlbum, doc.RootElement);

        Assert.Single(detail.Tracks);
    }
}
