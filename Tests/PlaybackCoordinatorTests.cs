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
            cancellationToken.ThrowIfCancellationRequested();

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
        private readonly List<Song> _items = [];
        private int _currentIndex = -1;

        public IReadOnlyList<Song> Items => _items;
        public int CurrentIndex => _currentIndex;
        public Song? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;
        public bool ShuffleEnabled => false;
        public RepeatMode RepeatMode => RepeatMode.Off;
        public event EventHandler? Changed;

        public void Load(params Song[] songs)
        {
            _items.Clear();
            _items.AddRange(songs);
            _currentIndex = songs.Length > 0 ? 0 : -1;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public Song? MoveNext()
        {
            if (_currentIndex < 0 || _items.Count == 0)
            {
                return null;
            }

            if (_currentIndex < _items.Count - 1)
            {
                _currentIndex++;
            }
            else
            {
                return null;
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        public Song? MovePrevious()
        {
            if (_currentIndex < 0 || _items.Count == 0)
            {
                return null;
            }

            if (_currentIndex > 0)
            {
                _currentIndex--;
            }
            else
            {
                return null;
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        public Song? PeekNext()
        {
            if (_currentIndex < 0 || _items.Count == 0)
            {
                return null;
            }

            var nextIndex = _currentIndex + 1;
            return nextIndex < _items.Count ? _items[nextIndex] : null;
        }

        public Song? PeekPlannedNext()
        {
            return PeekNext();
        }

        public void PlayNow(Song song) { }
        public void AddNext(Song song) { }
        public void AddToTheEnd(Song song) { }
        public void Remove(Song song) { }
        public void Clear() { }
        public void SetContext(IEnumerable<Song> songs, Song? startSong = null) { }
        public void SetQueue(IEnumerable<Song> songs, int startIndex) { }
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

        public List<PlayableTrack> PlayedTracks { get; } = [];
        public int StopCount { get; private set; }

        public Task PlayAsync(PlayableTrack track, CancellationToken cancellationToken = default)
        {
            PlayedTracks.Add(track);
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
            StopCount++;
            Snapshot = new PlaybackSnapshot { State = PlaybackState.Stopped };
            SnapshotChanged?.Invoke(this, new PlaybackStateChangedEventArgs(Snapshot));
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SetVolumeAsync(double volume, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void RaiseTrackEnded()
        {
            TrackEnded?.Invoke(this, EventArgs.Empty);
        }
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

    [Fact]
    public async Task TrackEnded_WithNextTrack_MovesQueueAndPlaysNext()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var queue = new FakeQueue();
        var coordinator = new PlaybackCoordinator(resolver, audio, queue);

        var first = new Song { Id = "1", Title = "A" };
        var second = new Song { Id = "2", Title = "B" };
        queue.Load(first, second);

        await coordinator.PlayQueueItemAsync();
        Assert.Equal("A", audio.Snapshot.CurrentTrack?.Title);

        audio.RaiseTrackEnded();
        // Give the async event handler a moment to run.
        await Task.Delay(50);

        Assert.Equal("B", audio.Snapshot.CurrentTrack?.Title);
        Assert.Equal(0, audio.StopCount);
    }

    [Fact]
    public async Task TrackEnded_WithNoNextTrack_StopsAudio()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var queue = new FakeQueue();
        var coordinator = new PlaybackCoordinator(resolver, audio, queue);

        queue.Load(new Song { Id = "1", Title = "A" });

        await coordinator.PlayQueueItemAsync();
        audio.RaiseTrackEnded();
        await Task.Delay(50);

        Assert.Equal(PlaybackState.Stopped, audio.Snapshot.State);
        Assert.Equal(1, audio.StopCount);
    }

    [Fact]
    public async Task TrackEnded_WithResolverFailure_DoesNotThrowAndStopsAudio()
    {
        var resolver = new SecondCallFailingResolver();
        var audio = new FakeAudio();
        var queue = new FakeQueue();
        var coordinator = new PlaybackCoordinator(resolver, audio, queue);

        var first = new Song { Id = "1", Title = "A" };
        var second = new Song { Id = "2", Title = "B" };
        queue.Load(first, second);

        await coordinator.PlayQueueItemAsync();
        audio.RaiseTrackEnded();

        // Should not throw from async event handler.
        await Task.Delay(50);

        Assert.True(audio.StopCount >= 1);
    }

    [Fact]
    public async Task PlayNextAsync_MovesQueueAndPlaysNext()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var queue = new FakeQueue();
        var coordinator = new PlaybackCoordinator(resolver, audio, queue);

        queue.Load(
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" });

        await coordinator.PlayQueueItemAsync();
        await coordinator.PlayNextAsync();

        Assert.Equal("B", audio.Snapshot.CurrentTrack?.Title);
    }

    [Fact]
    public async Task PlayPreviousAsync_MovesQueueAndPlaysPrevious()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var queue = new FakeQueue();
        var coordinator = new PlaybackCoordinator(resolver, audio, queue);

        queue.Load(
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" });

        await coordinator.PlayQueueItemAsync();
        await coordinator.PlayNextAsync();
        await coordinator.PlayPreviousAsync();

        Assert.Equal("A", audio.Snapshot.CurrentTrack?.Title);
    }

    [Fact]
    public async Task PlaySongAsync_RetriesOnceOnNormalException()
    {
        var resolver = new RetryableResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        var song = new Song { Id = "1", Title = "A" };

        await coordinator.PlaySongAsync(song);

        Assert.Equal(2, resolver.Calls);
        Assert.Equal("A", audio.Snapshot.CurrentTrack?.Title);
    }

    [Fact]
    public async Task PlaySongAsync_DoesNotRetryOnCancellation()
    {
        var resolver = new FakeResolver();
        var audio = new FakeAudio();
        var coordinator = new PlaybackCoordinator(resolver, audio, new FakeQueue());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => coordinator.PlaySongAsync(new Song { Id = "1", Title = "A" }, cts.Token));
    }

    private class FailingResolver : IPlaybackResolver
    {
        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Cannot resolve: no videoId");
        }
    }

    private class SecondCallFailingResolver : IPlaybackResolver
    {
        public int Calls { get; private set; }

        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Calls > 1)
            {
                throw new InvalidOperationException("Cannot resolve: no videoId");
            }

            return Task.FromResult(new PlayableTrack
            {
                Id = song.Id,
                Title = song.Title,
                SourceId = song.SourceId ?? "",
                StreamUri = new Uri("http://example.com/stream")
            });
        }
    }

    private class RetryableResolver : IPlaybackResolver
    {
        public int Calls { get; private set; }

        public Task<PlayableTrack> ResolveAsync(Song song, CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Calls == 1)
            {
                throw new InvalidOperationException("Stream expired");
            }

            return Task.FromResult(new PlayableTrack
            {
                Id = song.Id,
                Title = song.Title,
                SourceId = song.SourceId ?? "",
                StreamUri = new Uri("http://example.com/stream")
            });
        }
    }
}
