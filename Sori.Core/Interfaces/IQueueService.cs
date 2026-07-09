using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IQueueService
{
    IReadOnlyList<Song> Items { get; }
    int CurrentIndex { get; }
    Song? Current { get; }

    bool ShuffleEnabled { get; }
    RepeatMode RepeatMode { get; }

    event EventHandler? Changed;

    void PlayNow(Song song);
    void AddNext(Song song);
    void AddToTheEnd(Song song);
    void Remove(Song song);
    void Clear();

    void SetContext(IEnumerable<Song> songs, Song? startSong = null);
    void SetQueue(IEnumerable<Song> songs, int startIndex);

    Song? MoveNext();
    Song? MovePrevious();
    Song? PeekNext();

    /// <summary>
    /// Returns the planned next track. For non-shuffle this is the next track in order.
    /// For shuffle, if a next track is already planned it is returned; otherwise one is
    /// selected, stored as the planned next, and returned. This ensures coordinator and
    /// prefetch agree on what will play next.
    /// </summary>
    Song? PeekPlannedNext();

    void SetShuffle(bool enabled);
    void ToggleShuffle();

    void SetRepeatMode(RepeatMode mode);
    RepeatMode CycleRepeatMode();

    bool RadioEnabled { get; }
    int UserItemCount { get; }
    void ToggleRadio();
    void SetRadioQueue(IEnumerable<Song> songs);
}
