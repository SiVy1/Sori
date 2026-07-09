using LibVLCSharp.Shared;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace Sori.Playback;

public sealed class VlcAudioPlaybackService : IAudioPlaybackService, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly object _gate = new();
    private readonly System.Timers.Timer _positionTimer;

    private Media? _currentMedia;
    private PlaybackSnapshot _snapshot = new();

    public VlcAudioPlaybackService()
    {
        LibVLCSharp.Shared.Core.Initialize();

        _libVlc = new LibVLC("--no-video");
        _mediaPlayer = new MediaPlayer(_libVlc);

        _mediaPlayer.Playing += (_, _) => UpdateStatus(PlaybackState.Playing);
        _mediaPlayer.Paused += (_, _) => UpdateStatus(PlaybackState.Paused);
        _mediaPlayer.Stopped += (_, _) => UpdateStatus(PlaybackState.Stopped);
        _mediaPlayer.EndReached += (_, _) =>
        {
            // Do NOT update snapshot to Stopped here.
            // TrackEnded is handled by PlaybackCoordinator for auto-next.
            // Updating to Stopped would race with VM's SnapshotChanged handler.
            TrackEnded?.Invoke(this, EventArgs.Empty);
        };
        _mediaPlayer.EncounteredError += (_, _) => SetError("VLC encountered a playback error.");

        _positionTimer = new System.Timers.Timer(500);
        _positionTimer.Elapsed += (_, _) => OnPositionTick();
        _positionTimer.AutoReset = true;
    }

    public PlaybackSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? SnapshotChanged;

    public event EventHandler? TrackEnded;

    public Task PlayAsync(
        PlayableTrack track,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (track.StreamUri is null)
        {
            SetError("Playback source is not available for this track.");
            throw new InvalidOperationException(
                "Playback source is not available for this track.");
        }

        SetSnapshot(new PlaybackSnapshot
        {
            State = PlaybackState.Loading,
            CurrentTrack = track,
            Position = TimeSpan.Zero,
            Duration = null,
            Volume = Snapshot.Volume,
            ErrorMessage = null
        });

        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, track.StreamUri);
        _mediaPlayer.Media = _currentMedia;
        var started = _mediaPlayer.Play();

        if (!started)
        {
            SetError("VLC failed to start playback.");
            throw new InvalidOperationException("VLC failed to start playback.");
        }

        _positionTimer.Start();

        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }

        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Play();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _positionTimer.Stop();
        _mediaPlayer.Stop();
        _currentMedia?.Dispose();
        _currentMedia = null;

        var current = Snapshot;

        SetSnapshot(new PlaybackSnapshot
        {
            State = PlaybackState.Stopped,
            CurrentTrack = current.CurrentTrack,
            Position = TimeSpan.Zero,
            Duration = current.Duration,
            Volume = current.Volume,
            ErrorMessage = null
        });

        return Task.CompletedTask;
    }

    public Task SeekAsync(
        TimeSpan position,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        _mediaPlayer.Time = (long)position.TotalMilliseconds;

        RefreshPosition();

        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(
        double volume,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        volume = Math.Clamp(volume, 0.0, 1.0);

        _mediaPlayer.Volume = (int)Math.Round(volume * 100);

        var current = Snapshot;

        SetSnapshot(new PlaybackSnapshot
        {
            State = current.State,
            CurrentTrack = current.CurrentTrack,
            Position = current.Position,
            Duration = current.Duration,
            Volume = volume,
            ErrorMessage = current.ErrorMessage
        });

        return Task.CompletedTask;
    }

    private void UpdateStatus(PlaybackState state)
    {
        var current = Snapshot;

        SetSnapshot(new PlaybackSnapshot
        {
            State = state,
            CurrentTrack = current.CurrentTrack,
            Position = GetCurrentPosition(),
            Duration = GetDuration(),
            Volume = current.Volume,
            ErrorMessage = null
        });
    }

    private void RefreshPosition()
    {
        var current = Snapshot;

        SetSnapshot(new PlaybackSnapshot
        {
            State = current.State,
            CurrentTrack = current.CurrentTrack,
            Position = GetCurrentPosition(),
            Duration = GetDuration(),
            Volume = current.Volume,
            ErrorMessage = current.ErrorMessage
        });
    }

    private TimeSpan GetCurrentPosition()
    {
        var ms = _mediaPlayer.Time;

        return ms > 0
            ? TimeSpan.FromMilliseconds(ms)
            : TimeSpan.Zero;
    }

    private TimeSpan? GetDuration()
    {
        var ms = _mediaPlayer.Length;

        return ms > 0
            ? TimeSpan.FromMilliseconds(ms)
            : null;
    }

    private void SetError(string message)
    {
        var current = Snapshot;

        SetSnapshot(new PlaybackSnapshot
        {
            State = PlaybackState.Error,
            CurrentTrack = current.CurrentTrack,
            Position = GetCurrentPosition(),
            Duration = GetDuration(),
            Volume = current.Volume,
            ErrorMessage = message
        });
    }

    private void OnPositionTick()
    {
        var current = Snapshot;
        if (current.State != PlaybackState.Playing && current.State != PlaybackState.Paused)
            return;

        RefreshPosition();
    }

    private void SetSnapshot(PlaybackSnapshot snapshot)
    {
        lock (_gate)
        {
            _snapshot = snapshot;
        }

        SnapshotChanged?.Invoke(
            this,
            new PlaybackStateChangedEventArgs(snapshot));
    }

    public void Dispose()
    {
        _positionTimer.Stop();
        _positionTimer.Dispose();
        _currentMedia?.Dispose();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
