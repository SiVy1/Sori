using System.Text.Json;
using InnerTube.Browse;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Browse;

public class BrowseMapperPlaylistTests
{
    private readonly BrowseMapper _mapper = new();

    [Fact]
    public void MapPlaylist_WithMinimalPlaylistBrowseJson_ReturnsTracks()
    {
        var json = @"
        {
            ""header"": {
                ""musicEditablePlaylistDetailHeaderRenderer"": {
                    ""header"": {
                        ""musicDetailHeaderRenderer"": {
                            ""title"": { ""runs"": [{ ""text"": ""Test Playlist"" }] },
                            ""subtitle"": { ""runs"": [{ ""text"": ""Playlist"" }, { ""text"": "" • "" }, { ""text"": ""YouTube Music"" }] },
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
                                                                    ""text"": { ""runs"": [{ ""text"": ""Playlist Track 1"" }] }
                                                                }
                                                            },
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Artist 1"" }, { ""text"": "" • "" }, { ""text"": ""Album 1"" }] }
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
                                                        ""playlistItemData"": { ""videoId"": ""playlist1"" }
                                                    }
                                                },
                                                {
                                                    ""musicResponsiveListItemRenderer"": {
                                                        ""flexColumns"": [
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Playlist Track 2"" }] }
                                                                }
                                                            },
                                                            {
                                                                ""musicResponsiveListItemFlexColumnRenderer"": {
                                                                    ""text"": { ""runs"": [{ ""text"": ""Artist 2"" }] }
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
                                                        ""playlistItemData"": { ""videoId"": ""playlist2"" }
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
        var sourcePlaylist = new Playlist
        {
            Id = "youtubeMusic:playlist:PLtest",
            SourceId = "PLtest",
            Title = "Fallback Title"
        };

        var detail = _mapper.MapPlaylist(sourcePlaylist, doc.RootElement);

        Assert.Equal("Test Playlist", detail.Title);
        Assert.Equal("YouTube Music", detail.Subtitle);
        Assert.Equal("https://example.com/thumb-120.jpg", detail.ThumbnailUrl);
        Assert.Equal(2, detail.Tracks.Count);

        var track1 = detail.Tracks[0];
        Assert.Equal("youtubeMusic:track:playlist1", track1.Id);
        Assert.Equal("Playlist Track 1", track1.Title);
        Assert.Equal("Artist 1", track1.ArtistName);
        Assert.Equal("Album 1", track1.AlbumTitle);
        Assert.Equal(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(23), track1.Duration);

        var track2 = detail.Tracks[1];
        Assert.Equal("youtubeMusic:track:playlist2", track2.Id);
        Assert.Equal("Playlist Track 2", track2.Title);
        Assert.Equal("Artist 2", track2.ArtistName);
        Assert.Null(track2.AlbumTitle);
        Assert.Equal(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(5), track2.Duration);
    }

    [Fact]
    public void MapPlaylist_UsesSourcePlaylistFallbacks()
    {
        var json = "{ \"contents\": { \"twoColumnBrowseResultsRenderer\": { \"tabs\": [] } } }";
        using var doc = JsonDocument.Parse(json);

        var sourcePlaylist = new Playlist
        {
            Id = "youtubeMusic:playlist:PLtest",
            SourceId = "PLtest",
            Title = "Source Title",
            ThumbnailUrl = "https://example.com/source.jpg"
        };

        var detail = _mapper.MapPlaylist(sourcePlaylist, doc.RootElement);

        Assert.Equal("Source Title", detail.Title);
        Assert.Null(detail.Subtitle);
        Assert.Equal("https://example.com/source.jpg", detail.ThumbnailUrl);
        Assert.Empty(detail.Tracks);
    }

    [Fact]
    public void MapPlaylist_DoesNotReturnDuplicateTracks()
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
        var sourcePlaylist = new Playlist
        {
            Id = "youtubeMusic:playlist:PLtest",
            SourceId = "PLtest",
            Title = "Playlist"
        };

        var detail = _mapper.MapPlaylist(sourcePlaylist, doc.RootElement);

        Assert.Single(detail.Tracks);
    }
}
