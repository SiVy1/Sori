using Sori.Core.Models;

namespace Sori.Playback;

public static class YoutubeVideoIdExtractor
{
    public static string? GetVideoId(Song song)
    {
        if (!string.IsNullOrWhiteSpace(song.SourceId))
        {
            return song.SourceId;
        }

        const string prefix = "youtubeMusic:track:";

        if (song.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return song.Id[prefix.Length..];
        }

        return null;
    }
}
