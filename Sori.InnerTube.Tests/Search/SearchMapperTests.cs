using System.Text.Json;
using InnerTube.Search;
using Sori.Core.Models;

namespace Sori.InnerTube.Tests.Search;

public class SearchMapperTests
{
    private readonly SearchMapper _mapper = new();

    [Fact]
    public void Map_ReturnsNonEmptyResponse()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        Assert.NotNull(response);
        Assert.False(response.IsEmpty);
    }

    [Fact]
    public void Map_ClassifiesSongsCorrectly()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        Assert.NotEmpty(response.Songs);
        var song = response.Songs.First();
        Assert.Equal("youtubeMusic:track:UCCyoocDxBA", song.Id);
        Assert.Equal("Helena", song.Title);
        Assert.Equal("My Chemical Romance", song.ArtistName);
        Assert.Equal("Three Cheers for Sweet Revenge", song.AlbumTitle);
        Assert.Equal(TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(23), song.Duration);
        Assert.Equal("https://example.com/song-thumb-120.jpg", song.ThumbnailUrl);
    }

    [Fact]
    public void Map_ClassifiesAlbumsCorrectly()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        Assert.NotEmpty(response.Albums);
        var album = response.Albums.First();
        Assert.Equal("youtubeMusic:album:MPREb_abc123", album.Id);
        Assert.Equal("Three Cheers for Sweet Revenge", album.Title);
        Assert.Equal("My Chemical Romance", album.ArtistName);
        Assert.Equal("2004", album.Year);
        Assert.Equal("https://example.com/album-thumb-120.jpg", album.ThumbnailUrl);
    }

    [Fact]
    public void Map_ClassifiesArtistsCorrectly()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        Assert.NotEmpty(response.Artists);
        var artist = response.Artists.First();
        Assert.Equal("youtubeMusic:artist:UC_artist123", artist.Id);
        Assert.Equal("My Chemical Romance", artist.Name);
        Assert.Equal("https://example.com/artist-thumb-120.jpg", artist.ThumbnailUrl);
    }

    [Fact]
    public void Map_ClassifiesPlaylistsCorrectly()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        Assert.NotEmpty(response.Playlists);
        var playlist = response.Playlists.First();
        Assert.Equal("youtubeMusic:playlist:PLplaylist123", playlist.Id);
        Assert.Equal("My Chemical Romance Essentials", playlist.Title);
        Assert.Equal("https://example.com/playlist-thumb-120.jpg", playlist.ThumbnailUrl);
    }

    [Fact]
    public void Map_DoesNotClassifyEverythingAsSong()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        // Should have exactly 1 song, not 4
        Assert.Single(response.Songs);
        Assert.Single(response.Albums);
        Assert.Single(response.Artists);
        Assert.Single(response.Playlists);
    }

    [Fact]
    public void Map_RemovesDuplicateIds()
    {
        var json = LoadFixture("search-my-chemical.json");
        var response = _mapper.Map("my chemical romance", json.RootElement);

        var songIds = response.Songs.Select(s => s.Id).ToList();
        var albumIds = response.Albums.Select(a => a.Id).ToList();
        var artistIds = response.Artists.Select(a => a.Id).ToList();
        var playlistIds = response.Playlists.Select(p => p.Id).ToList();

        Assert.Equal(songIds.Count, songIds.Distinct().Count());
        Assert.Equal(albumIds.Count, albumIds.Distinct().Count());
        Assert.Equal(artistIds.Count, artistIds.Distinct().Count());
        Assert.Equal(playlistIds.Count, playlistIds.Distinct().Count());
    }

    private static JsonDocument LoadFixture(string name)
    {
        var path = Path.Combine("Fixtures", name);
        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json);
    }
}
