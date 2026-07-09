using Sori.Core.Interfaces;
using Sori.Core.Models;
using Sori.Playback;

namespace Tests;

public class PrefetchingPlaybackResolverTests
{
    private sealed class FakePlaybackResolver : IPlaybackResolver
    {
        public int Calls { get; private set; }

        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            Calls++;

            return Task.FromResult(new PlayableTrack
            {
                Id = song.Id,
                Title = song.Title,
                Source = "fake",
                SourceId = song.Id,
                StreamUri = new Uri("https://example.com/audio.mp3")
            });
        }
    }

    [Fact]
    public async Task PrefetchAsync_ThenResolveAsync_UsesCachedTrack()
    {
        var inner = new FakePlaybackResolver();
        var prefetch = new PrefetchingPlaybackResolver(inner);

        var song = new Song { Id = "test", Title = "Test" };

        await prefetch.PrefetchAsync(song);
        await Task.Delay(100); // let prefetch complete

        var result = await prefetch.ResolveAsync(song);

        Assert.Equal("Test", result.Title);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task ResolveAsync_WithoutPrefetch_CallsInnerResolver()
    {
        var inner = new FakePlaybackResolver();
        var prefetch = new PrefetchingPlaybackResolver(inner);

        var song = new Song { Id = "test", Title = "Test" };

        var result = await prefetch.ResolveAsync(song);

        Assert.Equal("Test", result.Title);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task PrefetchAsync_SameSongTwice_DoesNotDuplicateInnerCalls()
    {
        var inner = new FakePlaybackResolver();
        var prefetch = new PrefetchingPlaybackResolver(inner);

        var song = new Song { Id = "test", Title = "Test" };

        await prefetch.PrefetchAsync(song);
        await prefetch.PrefetchAsync(song);
        await Task.Delay(100);

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Clear_RemovesCachedTrack()
    {
        var inner = new FakePlaybackResolver();
        var prefetch = new PrefetchingPlaybackResolver(inner);

        var song = new Song { Id = "test", Title = "Test" };

        await prefetch.PrefetchAsync(song);
        await Task.Delay(100);

        prefetch.Clear();

        var result = await prefetch.ResolveAsync(song);

        Assert.Equal("Test", result.Title);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task PrefetchFailure_DoesNotCrashCaller()
    {
        var inner = new FailingPlaybackResolver();
        var prefetch = new PrefetchingPlaybackResolver(inner);

        var song = new Song { Id = "test", Title = "Test" };

        await prefetch.PrefetchAsync(song);
        await Task.Delay(100);

        // Resolve should fall back to inner and throw
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => prefetch.ResolveAsync(song));
    }

    private sealed class FailingPlaybackResolver : IPlaybackResolver
    {
        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Always fails");
        }
    }
}
