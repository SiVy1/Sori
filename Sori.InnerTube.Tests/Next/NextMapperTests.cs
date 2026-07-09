using System;
using System.Text.Json;
using InnerTube.Next;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Next;

public class NextMapperTests
{
    private readonly NextMapper _mapper = new();

    [Fact]
    public void Map_WithPlaylistPanelVideoRenderers_ReturnsCurrentAndItems()
    {
        var json = GetSyntheticNextJson();
        var sourceSong = new Song
        {
            Id = "youtubeMusic:track:video-current",
            SourceId = "video-current",
            Title = "Current Song",
            ArtistName = "Artist A"
        };

        var response = _mapper.Map(sourceSong, json.RootElement);

        Assert.NotNull(response);
        Assert.Equal("youtubeMusic:track:video-current", response.Current.Id);
        Assert.Equal("Current Song", response.Current.Title);
        Assert.Equal("Artist A", response.Current.ArtistName);

        Assert.Single(response.Items);
        var next = response.Items[0];
        Assert.Equal("youtubeMusic:track:video-next", next.Id);
        Assert.Equal("Next Song", next.Title);
        Assert.Equal("Artist B", next.ArtistName);
        Assert.Equal(TimeSpan.FromMinutes(4), next.Duration);
    }

    [Fact]
    public void Map_RemovesDuplicateSongs()
    {
        var json = @"
        {
          ""contents"": {
            ""singleColumnMusicWatchNextResultsRenderer"": {
              ""tabbedRenderer"": {
                ""watchNextTabbedResultsRenderer"": {
                  ""tabs"": [{
                    ""tabRenderer"": {
                      ""content"": {
                        ""musicQueueRenderer"": {
                          ""content"": {
                            ""playlistPanelRenderer"": {
                              ""contents"": [
                                {
                                  ""playlistPanelVideoRenderer"": {
                                    ""title"": { ""runs"": [{ ""text"": ""Current Song"" }] },
                                    ""longBylineText"": { ""runs"": [{ ""text"": ""Artist"" }] },
                                    ""lengthText"": { ""runs"": [{ ""text"": ""3:00"" }] },
                                    ""navigationEndpoint"": {
                                      ""watchEndpoint"": {
                                        ""videoId"": ""current-id"",
                                        ""playlistId"": ""RDAMVMcurrent""
                                      }
                                    }
                                  }
                                },
                                {
                                  ""playlistPanelVideoRenderer"": {
                                    ""title"": { ""runs"": [{ ""text"": ""Next Song A"" }] },
                                    ""longBylineText"": { ""runs"": [{ ""text"": ""Artist"" }] },
                                    ""lengthText"": { ""runs"": [{ ""text"": ""3:00"" }] },
                                    ""navigationEndpoint"": {
                                      ""watchEndpoint"": {
                                        ""videoId"": ""dup-id"",
                                        ""playlistId"": ""RDAMVMdup""
                                      }
                                    }
                                  }
                                },
                                {
                                  ""playlistPanelVideoRenderer"": {
                                    ""title"": { ""runs"": [{ ""text"": ""Next Song B"" }] },
                                    ""longBylineText"": { ""runs"": [{ ""text"": ""Artist"" }] },
                                    ""lengthText"": { ""runs"": [{ ""text"": ""4:00"" }] },
                                    ""navigationEndpoint"": {
                                      ""watchEndpoint"": {
                                        ""videoId"": ""dup-id"",
                                        ""playlistId"": ""RDAMVMdup""
                                      }
                                    }
                                  }
                                }
                              ]
                            }
                          }
                        }
                      }
                    }
                  }]
                }
              }
            }
          }
        }";

        using var doc = JsonDocument.Parse(json);
        var sourceSong = new Song
        {
            Id = "youtubeMusic:track:current-id",
            SourceId = "current-id",
            Title = "Current Song",
            ArtistName = "Artist"
        };

        var response = _mapper.Map(sourceSong, doc.RootElement);

        // 3 renderers: 1 current + 2 duplicates -> after dedup by id: 2 items
        // -> current removed: 1 item (dup-id) in nextItems
        Assert.Single(response.Items);
        Assert.Equal("youtubeMusic:track:dup-id", response.Items[0].Id);
    }

    [Fact]
    public void Map_ExtractsPlaylistId()
    {
        var json = GetSyntheticNextJson();
        var sourceSong = new Song
        {
            Id = "youtubeMusic:track:video-current",
            SourceId = "video-current",
            Title = "Current Song",
            ArtistName = "Artist A"
        };

        var response = _mapper.Map(sourceSong, json.RootElement);

        Assert.Equal("RDAMVMvideo-current", response.PlaylistId);
    }

    [Fact]
    public void Map_HandlesMissingLyricsAndRelated()
    {
        var json = GetSyntheticNextJson();
        var sourceSong = new Song
        {
            Id = "youtubeMusic:track:video-current",
            SourceId = "video-current",
            Title = "Current Song",
            ArtistName = "Artist A"
        };

        var response = _mapper.Map(sourceSong, json.RootElement);

        Assert.Null(response.LyricsBrowseId);
        Assert.Null(response.RelatedBrowseId);
    }

    private static JsonDocument GetSyntheticNextJson()
    {
        var json = @"
        {
          ""contents"": {
            ""singleColumnMusicWatchNextResultsRenderer"": {
              ""tabbedRenderer"": {
                ""watchNextTabbedResultsRenderer"": {
                  ""tabs"": [
                    {
                      ""tabRenderer"": {
                        ""content"": {
                          ""musicQueueRenderer"": {
                            ""content"": {
                              ""playlistPanelRenderer"": {
                                ""contents"": [
                                  {
                                    ""playlistPanelVideoRenderer"": {
                                      ""title"": { ""runs"": [{ ""text"": ""Current Song"" }] },
                                      ""longBylineText"": { ""runs"": [{ ""text"": ""Artist A"" }] },
                                      ""lengthText"": { ""runs"": [{ ""text"": ""3:00"" }] },
                                      ""thumbnail"": {
                                        ""thumbnails"": [
                                          { ""url"": ""https://example.com/thumb-60.jpg"", ""width"": 60, ""height"": 60 },
                                          { ""url"": ""https://example.com/thumb-120.jpg"", ""width"": 120, ""height"": 120 }
                                        ]
                                      },
                                      ""navigationEndpoint"": {
                                        ""watchEndpoint"": {
                                          ""videoId"": ""video-current"",
                                          ""playlistId"": ""RDAMVMvideo-current""
                                        }
                                      },
                                      ""selected"": true
                                    }
                                  },
                                  {
                                    ""playlistPanelVideoRenderer"": {
                                      ""title"": { ""runs"": [{ ""text"": ""Next Song"" }] },
                                      ""longBylineText"": { ""runs"": [{ ""text"": ""Artist B"" }] },
                                      ""lengthText"": { ""runs"": [{ ""text"": ""4:00"" }] },
                                      ""thumbnail"": {
                                        ""thumbnails"": [
                                          { ""url"": ""https://example.com/next-60.jpg"", ""width"": 60, ""height"": 60 }
                                        ]
                                      },
                                      ""navigationEndpoint"": {
                                        ""watchEndpoint"": {
                                          ""videoId"": ""video-next"",
                                          ""playlistId"": ""RDAMVMvideo-current""
                                        }
                                      }
                                    }
                                  }
                                ]
                              }
                            }
                          }
                        }
                      }
                    }
                  ]
                }
              }
            }
          }
        }";

        return JsonDocument.Parse(json);
    }
}
