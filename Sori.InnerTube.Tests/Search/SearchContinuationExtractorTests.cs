using System.Text.Json;
using InnerTube.Search;
using Sori.Core.Enums;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Search;

public class SearchContinuationExtractorTests
{
    [Fact]
    public void FindsNextContinuationData()
    {
        var json = @"
        {
          ""contents"": {
            ""tabbedSearchResultsRenderer"": {
              ""tabs"": [{
                ""tabRenderer"": {
                  ""content"": {
                    ""sectionListRenderer"": {
                      ""contents"": [{
                        ""musicShelfRenderer"": {
                          ""contents"": [],
                          ""continuations"": [{
                            ""nextContinuationData"": {
                              ""continuation"": ""TOKEN_123""
                            }
                          }]
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
        var token = SearchContinuationExtractor.FindMusicShelfContinuationToken(doc.RootElement);

        Assert.Equal("TOKEN_123", token);
    }

    [Fact]
    public void FindsContinuationItemRendererToken()
    {
        var json = @"
        {
          ""continuationContents"": {
            ""musicShelfContinuation"": {
              ""contents"": [{
                ""musicResponsiveListItemRenderer"": {}
              }],
              ""continuations"": [{
                ""nextContinuationData"": {
                  ""continuation"": ""TOKEN_456""
                }
              }]
            }
          }
        }";

        using var doc = JsonDocument.Parse(json);
        var token = SearchContinuationExtractor.FindContinuationTokenInRenderer(doc.RootElement);

        Assert.Equal("TOKEN_456", token);
    }

    [Fact]
    public void ReturnsNullWhenNoContinuation()
    {
        using var doc = JsonDocument.Parse("{}");
        var token = SearchContinuationExtractor.FindMusicShelfContinuationToken(doc.RootElement);

        Assert.Null(token);
    }
}

public class SearchMapperMergeTests
{
    [Fact]
    public void MergeForFilter_Playlists_DeduplicatesAndLimits()
    {
        var first = new SearchResponse
        {
            Playlists = [new Playlist { Id = "pl:1", Title = "A" }, new Playlist { Id = "pl:2", Title = "B" }]
        };
        var second = new SearchResponse
        {
            Playlists = [new Playlist { Id = "pl:2", Title = "B" }, new Playlist { Id = "pl:3", Title = "C" }]
        };

        var merged = SearchMapper.MergeForFilter(first, second, SearchFilter.Playlists, 2);

        Assert.Equal(2, merged.Playlists.Count);
        Assert.Equal("A", merged.Playlists[0].Title);
        Assert.Equal("B", merged.Playlists[1].Title);
    }

    [Fact]
    public void CountForFilter_Songs_ReturnsSongCount()
    {
        var response = new SearchResponse
        {
            Songs = [new Song { Id = "1" }, new Song { Id = "2" }],
            Albums = [new Album { Id = "a:1", ArtistName = "", Title = "", Year = "" }]
        };

        var count = SearchMapper.CountForFilter(response, SearchFilter.Songs);
        Assert.Equal(2, count);
    }
}
