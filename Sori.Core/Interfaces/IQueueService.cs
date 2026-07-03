using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IQueueService
{
    IReadOnlyList<Song> Items { get; }

    Song? Current { get; }

    void PlayNow(Song song);
    void AddNext(Song song);
    void AddToTheEnd(Song song);
    void Remove(Song song);
    void Clear();

    Song? MoveNext();
    Song? MovePrevious();
}