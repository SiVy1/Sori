using Sori.Core.Enums;

namespace Sori.Core.Models;

public sealed class PlaybackSnapshot
{
    public PlaybackState State { get; init; } = PlaybackState.Stopped;

    public PlayableTrack? CurrentTrack { get; init; }

    public TimeSpan Position { get; init; }
    public TimeSpan? Duration { get; init; }

    public double Volume { get; init; } = 1.0;

    public string? ErrorMessage { get; init; }
}
