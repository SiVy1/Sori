using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Next;

public sealed class InnerTubeUpNextService : IUpNextService
{
    private readonly InnerTubeClient _client;
    private readonly InnerTubeContextFactory _contextFactory;
    private readonly NextMapper _mapper;

    public InnerTubeUpNextService(
        InnerTubeClient client,
        InnerTubeContextFactory contextFactory,
        NextMapper mapper)
    {
        _client = client;
        _contextFactory = contextFactory;
        _mapper = mapper;
    }

    public async Task<UpNextResponse> GetUpNextAsync(
        Song song,
        CancellationToken cancellationToken = default)
    {
        var videoId = GetVideoId(song);

        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new InvalidOperationException(
                "Cannot load Up Next because song does not contain a YouTube videoId.");
        }

        var body = new
        {
            context = _contextFactory.CreateContext(),
            videoId,
            isAudioOnly = true,
            enablePersistentPlaylistPanel = true,
            tunerSettingValue = "AUTOMIX_SETTING_NORMAL"
        };

        using var json = await _client.PostAsync(
            "next",
            body,
            cancellationToken);

        var response = _mapper.Map(song, json.RootElement);

        // ponytail: /next for a single song only returns the current song + a radio playlistId.
        // If no up-next items were found, try /next again with the playlistId to fetch the radio queue.
        if (response.Items.Count == 0 && !string.IsNullOrWhiteSpace(response.PlaylistId))
        {
            var playlistBody = new
            {
                context = _contextFactory.CreateContext(),
                playlistId = response.PlaylistId,
                isAudioOnly = true
            };

            using var playlistJson = await _client.PostAsync(
                "next",
                playlistBody,
                cancellationToken);

            // ponytail: debug dump for the playlist /next call
            var raw = playlistJson.RootElement.ToString();
            var dumpPath = Path.Combine(Path.GetTempPath(), "sori-next-playlist-response.json");
            File.WriteAllText(dumpPath, raw);

            var playlistResponse = _mapper.Map(song, playlistJson.RootElement);

            // Merge: keep the original current song, but replace items with playlist results
            return new UpNextResponse
            {
                Current = response.Current,
                Items = playlistResponse.Items,
                PlaylistId = response.PlaylistId,
                LyricsBrowseId = response.LyricsBrowseId,
                RelatedBrowseId = response.RelatedBrowseId
            };
        }

        return response;
    }

    private static string? GetVideoId(Song song)
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
