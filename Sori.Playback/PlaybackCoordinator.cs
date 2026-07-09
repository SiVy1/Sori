using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace Sori.Playback;

public sealed class PlaybackCoordinator : IPlaybackCoordinator
{
    private readonly IPlaybackResolver _resolver;
    private readonly IAudioPlaybackService _audio;
    private readonly IQueueService _queue;
    private readonly IPrefetchingPlaybackResolver? _prefetchResolver;

    public PlaybackCoordinator(
        IPlaybackResolver resolver,
        IAudioPlaybackService audio,
        IQueueService queue,
        IPrefetchingPlaybackResolver? prefetchResolver = null)
    {
        _resolver = resolver;
        _audio = audio;
        _queue = queue;
        _prefetchResolver = prefetchResolver;

        _audio.SnapshotChanged += (_, args) =>
        {
            SnapshotChanged?.Invoke(this, args);
        };

        _audio.TrackEnded += async (_, _) =>
        {
            try
            {
                var next = _queue.MoveNext();
                if (next is not null)
                {
                    await PlaySongWithRetryAsync(next);
                }
                else
                {
                    await _audio.StopAsync();
                }
            }
            catch (Exception)
            {
                // Ensure playback stops safely even if resolving/playing the next
                // track fails after retry. The error is reflected in the snapshot.
                try
                {
                    await _audio.StopAsync();
                }
                catch
                {
                    // ignored
                }
            }
        };
    }

    public PlaybackSnapshot Snapshot => _audio.Snapshot;

    public event EventHandler<PlaybackStateChangedEventArgs>? SnapshotChanged;

    public async Task PlaySongAsync(
        Song song,
        CancellationToken cancellationToken = default)
    {
        await PlaySongWithRetryAsync(song, cancellationToken);
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        return _audio.PauseAsync(cancellationToken);
    }

    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        return _audio.ResumeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _audio.StopAsync(cancellationToken);
    }

    public Task SeekAsync(
        TimeSpan position,
        CancellationToken cancellationToken = default)
    {
        return _audio.SeekAsync(position, cancellationToken);
    }

    public Task SetVolumeAsync(
        double volume,
        CancellationToken cancellationToken = default)
    {
        return _audio.SetVolumeAsync(volume, cancellationToken);
    }

    public async Task PlayNextAsync(CancellationToken cancellationToken = default)
    {
        var next = _queue.MoveNext();
        if (next is not null)
        {
            await PlaySongWithRetryAsync(next, cancellationToken);
            await PrefetchNextAsync(cancellationToken);
        }
    }

    public async Task PlayPreviousAsync(CancellationToken cancellationToken = default)
    {
        var previous = _queue.MovePrevious();
        if (previous is not null)
        {
            await PlaySongWithRetryAsync(previous, cancellationToken);
            await PrefetchNextAsync(cancellationToken);
        }
    }

    public async Task PlayQueueItemAsync(CancellationToken cancellationToken = default)
    {
        var current = _queue.Current;

        if (current is null)
        {
            return;
        }

        await PlaySongWithRetryAsync(current, cancellationToken);
        await PrefetchNextAsync(cancellationToken);
    }

    private async Task PrefetchNextAsync(CancellationToken cancellationToken = default)
    {
        if (_prefetchResolver is null)
        {
            return;
        }

        var next = _queue.PeekPlannedNext();

        if (next is null)
        {
            return;
        }

        try
        {
            await _prefetchResolver.PrefetchAsync(next, cancellationToken);
        }
        catch
        {
            // Prefetch failures must never break playback.
        }
    }

    private async Task PlaySongWithRetryAsync(
        Song song,
        CancellationToken cancellationToken = default,
        bool isRetry = false)
    {
        try
        {
            var playable = await _resolver.ResolveAsync(song, cancellationToken);
            await _audio.PlayAsync(playable, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (!isRetry)
            {
                // Stream may have expired; try once more
                await PlaySongWithRetryAsync(song, cancellationToken, isRetry: true);
                return;
            }

            throw;
        }
    }
}
