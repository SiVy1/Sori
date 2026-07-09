using Sori.Core.Interfaces;
using Sori.Core.Models;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Sori.Playback;

public sealed class YoutubeExplodePlaybackResolver : IPlaybackResolver
{
    private readonly YoutubeClient _youtube;

    public YoutubeExplodePlaybackResolver()
        : this(new YoutubeClient())
    {
    }

    public YoutubeExplodePlaybackResolver(YoutubeClient youtube)
    {
        _youtube = youtube;
    }

    public async Task<PlayableTrack> ResolveAsync(
        Song song,
        CancellationToken cancellationToken = default)
    {
        var videoId = YoutubeVideoIdExtractor.GetVideoId(song);

        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new InvalidOperationException(
                "Cannot play this track because it does not contain a YouTube videoId.");
        }

        var manifest = await _youtube.Videos.Streams.GetManifestAsync(
            videoId,
            cancellationToken);

        var streamInfo = manifest
            .GetAudioOnlyStreams()
            .OrderByDescending(x => x.Bitrate.BitsPerSecond)
            .FirstOrDefault();

        if (streamInfo is null)
        {
            throw new InvalidOperationException(
                "No audio-only stream was found for this YouTube track.");
        }

        return new PlayableTrack
        {
            Id = song.Id,
            Title = song.Title,
            ArtistName = song.ArtistName,
            AlbumTitle = song.AlbumTitle,
            ThumbnailUrl = song.ThumbnailUrl,
            Source = "youtube",
            SourceId = videoId,
            StreamUri = new Uri(streamInfo.Url)
        };
    }
}
