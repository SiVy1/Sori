using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.ViewModels;

public partial class PlayerBarViewModel : ObservableObject
{
    private readonly IPlaybackCoordinator _playbackCoordinator;
    private readonly IQueueService _queueService;

    [ObservableProperty] private PlaybackSnapshot currentPlaybackSnapshot = new();

    [ObservableProperty] private string? playbackError;

    [ObservableProperty] private double playbackPositionSeconds;

    [ObservableProperty] private double playbackDurationSeconds;

    [ObservableProperty] private double playbackVolumePercent = 100.0;

    [ObservableProperty] private bool isPlaybackLoading;

    [ObservableProperty] private string queueIndexText = "";

    private bool _isUpdatingPlaybackPosition;
    private CancellationTokenSource? _seekDebounceCts;

    public PlayerBarViewModel(
        IPlaybackCoordinator playbackCoordinator,
        IQueueService queueService)
    {
        _playbackCoordinator = playbackCoordinator;
        _queueService = queueService;

        _playbackCoordinator.SnapshotChanged += (_, args) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentPlaybackSnapshot = args.Snapshot;
            });
        };

        _queueService.Changed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(UpdateQueueIndexText);
        };

        TogglePlayPauseCommand = new AsyncRelayCommand(TogglePlayPauseAsync);
        StopCommand = new AsyncRelayCommand(StopAsync);
        NextCommand = new AsyncRelayCommand(NextAsync);
        PreviousCommand = new AsyncRelayCommand(PreviousAsync);

        UpdateQueueIndexText();
    }

    public bool HasPlaybackError => !string.IsNullOrWhiteSpace(PlaybackError);

    public string CurrentTrackTitle =>
        CurrentPlaybackSnapshot.CurrentTrack?.Title ?? "Nothing playing";

    public string CurrentTrackArtist =>
        CurrentPlaybackSnapshot.CurrentTrack?.ArtistName ?? "";

    public string PlaybackStatusText =>
        CurrentPlaybackSnapshot.State.ToString();

    public string PlaybackPositionText => FormatTime(TimeSpan.FromSeconds(PlaybackPositionSeconds));

    public string PlaybackDurationText =>
        PlaybackDurationSeconds > 0
            ? FormatTime(TimeSpan.FromSeconds(PlaybackDurationSeconds))
            : "--:--";

    public bool CanSeek => PlaybackDurationSeconds > 0;

    public bool IsPlaying => CurrentPlaybackSnapshot.State == PlaybackState.Playing;

    public IAsyncRelayCommand TogglePlayPauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand NextCommand { get; }
    public IAsyncRelayCommand PreviousCommand { get; }

    partial void OnCurrentPlaybackSnapshotChanged(PlaybackSnapshot value)
    {
        _isUpdatingPlaybackPosition = true;

        PlaybackPositionSeconds = value.Position.TotalSeconds;
        PlaybackDurationSeconds = value.Duration?.TotalSeconds ?? 0;
        PlaybackVolumePercent = Math.Clamp(value.Volume * 100.0, 0, 100);

        _isUpdatingPlaybackPosition = false;

        OnPropertyChanged(nameof(CurrentTrackTitle));
        OnPropertyChanged(nameof(CurrentTrackArtist));
        OnPropertyChanged(nameof(PlaybackStatusText));
        OnPropertyChanged(nameof(PlaybackPositionText));
        OnPropertyChanged(nameof(PlaybackDurationText));
        OnPropertyChanged(nameof(CanSeek));
        OnPropertyChanged(nameof(IsPlaying));
    }

    partial void OnPlaybackPositionSecondsChanged(double value)
    {
        if (_isUpdatingPlaybackPosition) return;

        _seekDebounceCts?.Cancel();
        _seekDebounceCts = new CancellationTokenSource();
        var token = _seekDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!token.IsCancellationRequested)
                {
                    await _playbackCoordinator.SeekAsync(TimeSpan.FromSeconds(value));
                }
            }
            catch (OperationCanceledException)
            {
                // debounce cancelled
            }
        }, token);
    }

    partial void OnPlaybackVolumePercentChanged(double value)
    {
        if (_isUpdatingPlaybackPosition) return;

        _ = _playbackCoordinator.SetVolumeAsync(value / 100.0);
    }

    partial void OnPlaybackErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPlaybackError));
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{time.Hours:D1}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    private void UpdateQueueIndexText()
    {
        var current = _queueService.Current;
        var count = _queueService.Items.Count;

        QueueIndexText = current is not null && count > 0
            ? $"{_queueService.CurrentIndex + 1} / {count}"
            : "";
    }

    private async Task TogglePlayPauseAsync()
    {
        try
        {
            PlaybackError = null;
            if (IsPlaying)
                await _playbackCoordinator.PauseAsync();
            else
                await _playbackCoordinator.ResumeAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
    }

    private async Task StopAsync()
    {
        try
        {
            PlaybackError = null;
            await _playbackCoordinator.StopAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
    }

    private async Task NextAsync()
    {
        try
        {
            PlaybackError = null;
            IsPlaybackLoading = true;
            await _playbackCoordinator.PlayNextAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
        finally
        {
            IsPlaybackLoading = false;
        }
    }

    private async Task PreviousAsync()
    {
        try
        {
            PlaybackError = null;
            IsPlaybackLoading = true;
            await _playbackCoordinator.PlayPreviousAsync();
        }
        catch (Exception ex)
        {
            PlaybackError = ex.Message;
        }
        finally
        {
            IsPlaybackLoading = false;
        }
    }
}
