using Sori.Core.Enums;
using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IPlaybackService
{
    Song? CurrentSong { get; }
    PlaybackState State { get; }

    Task PlayAsync(Song song, CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}