using System;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Browse;

public sealed class InnerTubeCollectionService : ICollectionService
{
    private readonly InnerTubeClient _client;
    private readonly InnerTubeContextFactory _contextFactory;
    private readonly BrowseMapper _mapper;

    public InnerTubeCollectionService(
        InnerTubeClient client,
        InnerTubeContextFactory contextFactory,
        BrowseMapper mapper)
    {
        _client = client;
        _contextFactory = contextFactory;
        _mapper = mapper;
    }

    public async Task<CollectionDetail> GetAlbumAsync(
        Album album,
        CancellationToken cancellationToken = default)
    {
        var browseId = album.SourceId;

        if (string.IsNullOrWhiteSpace(browseId))
        {
            browseId = album.Id.Replace("youtubeMusic:album:", "");
        }

        if (string.IsNullOrWhiteSpace(browseId))
        {
            throw new InvalidOperationException("Album does not contain a browseId.");
        }

        var body = new
        {
            context = _contextFactory.CreateContext(),
            browseId
        };

        using var json = await _client.PostAsync(
            "browse",
            body,
            cancellationToken);

        return _mapper.MapAlbum(album, json.RootElement);
    }

    public async Task<CollectionDetail> GetPlaylistAsync(
        Playlist playlist,
        CancellationToken cancellationToken = default)
    {
        var browseId = playlist.SourceId;

        if (string.IsNullOrWhiteSpace(browseId))
        {
            browseId = playlist.Id.Replace("youtubeMusic:playlist:", "");
        }

        if (!string.IsNullOrWhiteSpace(browseId) &&
            !browseId.StartsWith("VL", StringComparison.OrdinalIgnoreCase))
        {
            browseId = "VL" + browseId;
        }

        if (string.IsNullOrWhiteSpace(browseId))
        {
            throw new InvalidOperationException("Playlist does not contain a browseId.");
        }

        var body = new
        {
            context = _contextFactory.CreateContext(),
            browseId
        };

        using var json = await _client.PostAsync(
            "browse",
            body,
            cancellationToken);

        return _mapper.MapPlaylist(playlist, json.RootElement);
    }

    public async Task<ArtistDetail> GetArtistAsync(
        Artist artist,
        CancellationToken cancellationToken = default)
    {
        var browseId = artist.SourceId;

        if (string.IsNullOrWhiteSpace(browseId))
        {
            browseId = artist.Id.Replace("youtubeMusic:artist:", "");
        }

        if (string.IsNullOrWhiteSpace(browseId))
        {
            throw new InvalidOperationException("Artist does not contain a browseId.");
        }

        var body = new
        {
            context = _contextFactory.CreateContext(),
            browseId
        };

        using var json = await _client.PostAsync(
            "browse",
            body,
            cancellationToken);

        return _mapper.MapArtist(artist, json.RootElement);
    }
}
