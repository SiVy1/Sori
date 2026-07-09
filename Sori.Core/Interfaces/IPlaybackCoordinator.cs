using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IPlaybackCoordinator
{
    PlaybackSnapshot Snapshot { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? SnapshotChanged;

    Task PlaySongAsync(
        Song song,
        CancellationToken cancellationToken = default);

    Task PauseAsync(CancellationToken cancellationToken = default);

    Task ResumeAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task SeekAsync(
        TimeSpan position,
        CancellationToken cancellationToken = default);

    Task SetVolumeAsync(
        double volume,
        CancellationToken cancellationToken = default);

    Task PlayNextAsync(CancellationToken cancellationToken = default);

    Task PlayPreviousAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays the current item in the queue, then prefetches the planned next item.
    /// </summary>
    Task PlayQueueItemAsync(CancellationToken cancellationToken = default);
}
