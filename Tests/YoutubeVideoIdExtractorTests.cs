using Sori.Core.Models;
using Sori.Playback;

namespace Tests;

public class YoutubeVideoIdExtractorTests
{
    [Fact]
    public void WithSourceId_ReturnsSourceId()
    {
        var song = new Song
        {
            Id = "youtubeMusic:track:abc123",
            SourceId = "rawId",
            Title = "Test"
        };

        var result = YoutubeVideoIdExtractor.GetVideoId(song);

        Assert.Equal("rawId", result);
    }

    [Fact]
    public void WithYoutubeMusicTrackId_ReturnsVideoId()
    {
        var song = new Song
        {
            Id = "youtubeMusic:track:dQw4w9WgXcQ",
            Title = "Test"
        };

        var result = YoutubeVideoIdExtractor.GetVideoId(song);

        Assert.Equal("dQw4w9WgXcQ", result);
    }

    [Fact]
    public void WithUnknownId_ReturnsNull()
    {
        var song = new Song
        {
            Id = "spotify:track:123",
            Title = "Test"
        };

        var result = YoutubeVideoIdExtractor.GetVideoId(song);

        Assert.Null(result);
    }
}
