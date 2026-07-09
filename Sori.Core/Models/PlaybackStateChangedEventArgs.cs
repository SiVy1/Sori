using Sori.Core.Models;

namespace Sori.Core.Models;

public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackStateChangedEventArgs(PlaybackSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public PlaybackSnapshot Snapshot { get; }
}
