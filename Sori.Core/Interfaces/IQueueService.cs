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

    void SetShuffle(bool enabled);
    void ToggleShuffle();

    void SetRepeatMode(RepeatMode mode);
    RepeatMode CycleRepeatMode();
}
