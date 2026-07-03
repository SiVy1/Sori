using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Dev;

public sealed class MockPlaybackService : IPlaybackService
{
    public Song? CurrentSong { get; private set; }
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public Task PlayAsync(Song song, CancellationToken cancellationToken = default)
    {
        CurrentSong = song;
        State = PlaybackState.Playing;
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (State == PlaybackState.Playing) State = PlaybackState.Paused;

        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentSong is not null) State = PlaybackState.Playing;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        CurrentSong = null;
        State = PlaybackState.Stopped;
        return Task.CompletedTask;
    }
}