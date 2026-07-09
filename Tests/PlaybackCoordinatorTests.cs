using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;
using Sori.Playback;

namespace Tests;

public class PlaybackCoordinatorTests
{
    private class FakeResolver : IPlaybackResolver
    {
        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PlayableTrack
            {
                Id = song.Id,
                Title = song.Title,
                SourceId = song.SourceId ?? "",
                StreamUri = new Uri("http://example.com/stream")
            });
        }
    }

    private class FakeQueue : IQueueService
    {
        public IReadOnlyList<Song> Items => Array.Empty<Song>();
        public int CurrentIndex => -1;
        public Song? Current => null;
        public bool ShuffleEnabled => false;
        public RepeatMode RepeatMode => RepeatMode.Off;
        public event EventHandler? Changed;
        public void PlayNow(Song song) { }
        public void AddNext(Song song) { }
        public void AddToTheEnd(Song song) { }
        public void Remove(Song song) { }
        public void Clear() { }
        public void SetContext(IEnumerable<Song> songs, Song? startSong = null) { }
        public void SetQueue(IEnumerable<Song> songs, int startIndex) { }
        public Song? MoveNext() => null;
        public Song? MovePrevious() => null;
        public Song? PeekNext() => null;
        public void SetShuffle(bool enabled) { }
        public void ToggleShuffle() { }
        public void SetRepeatMode(RepeatMode mode) { }
        public RepeatMode CycleRepeatMode() => RepeatMode.Off;
    }

    private class FakeAudio : IAudioPlaybackService
    {
        public PlaybackSnapshot Snapshot { get; private set; } = new();
        public event EventHandler<PlaybackStateChangedEventArgs>? SnapshotChanged;
        public event EventHandler? TrackEnded;

        public Task PlayAsync(PlayableTrack track, CancellationToken cancellationToken = default)
        {
            Snapshot = new PlaybackSnapshot
            {
                State = PlaybackState.Playing,
                CurrentTrack = track
            };
            SnapshotChanged?.Invoke(this, new PlaybackStateChangedEventArgs(Snapshot));
            return Task.CompletedTask;
        }

        public Task PauseAsync(CancellationToken cancellationToken = default)
        {
            Snapshot = new PlaybackSnapshot { State = PlaybackState.Paused };
            SnapshotChanged?.Invoke(this, new PlaybackStateChangedEventArgs(Snapshot));
            return Task.CompletedTask;
        }

        public Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            Snapshot = new PlaybackSnapshot { State = PlaybackState.Playing };
            SnapshotChanged?.Invoke(this, new PlaybackStateChangedEventArgs(Snapshot));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Snapshot = new PlaybackSnapshot { State = PlaybackState.Stopped };
            SnapshotChanged?.Invoke(this, new PlaybackStateChangedEventArgs(Snapshot));
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetVolumeAsync(double volume, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task PlaySongAsync_CallsResolverThenAudio()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        var song = new Song
        {
            Id = "youtubeMusic:track:test",
            SourceId = "test",
            Title = "Test Song"
        };

        await coordinator.PlaySongAsync(song);

        Assert.Equal(PlaybackState.Playing, coordinator.Snapshot.State);
        Assert.NotNull(coordinator.Snapshot.CurrentTrack);
        Assert.Equal("Test Song", coordinator.Snapshot.CurrentTrack!.Title);
    }

    [Fact]
    public async Task PauseAsync_CallsAudioPause()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        await coordinator.PauseAsync();

        Assert.Equal(PlaybackState.Paused, coordinator.Snapshot.State);
    }

    [Fact]
    public async Task ResumeAsync_CallsAudioResume()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        await coordinator.ResumeAsync();

        Assert.Equal(PlaybackState.Playing, coordinator.Snapshot.State);
    }

    [Fact]
    public async Task StopAsync_CallsAudioStop()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        await coordinator.StopAsync();

        Assert.Equal(PlaybackState.Stopped, coordinator.Snapshot.State);
    }

    [Fact]
    public async Task PlaySongAsync_PropagatesResolverError()
    {
        var resolver = new FailingResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        var song = new Song
        {
            Id = "unknown",
            Title = "Test"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.PlaySongAsync(song));

        Assert.Contains("videoId", ex.Message);
    }

    private class FailingResolver : IPlaybackResolver
    {
        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Cannot resolve: no videoId");
        }
    }
}
