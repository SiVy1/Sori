using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IAudioPlaybackService
{
    PlaybackSnapshot Snapshot { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? SnapshotChanged;

    event EventHandler? TrackEnded;

    Task PlayAsync(
        PlayableTrack track,
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
}
