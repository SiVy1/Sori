using System;
using System.Collections.Generic;
using System.Linq;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

public sealed class QueueService : IQueueService
{
    private readonly List<Song> _items = [];
    private readonly Random _random = new();
    private readonly List<string> _shuffleHistory = [];

    private int _currentIndex = -1;
    private string? _plannedShuffleNextId;

    public IReadOnlyList<Song> Items => _items;

    public int CurrentIndex => _currentIndex;

    public Song? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    public bool ShuffleEnabled { get; private set; }

    public RepeatMode RepeatMode { get; private set; } = RepeatMode.Off;

    public event EventHandler? Changed;

    public void PlayNow(Song song)
    {
        var existingIndex = _items.FindIndex(x => x.Id == song.Id);

        if (existingIndex >= 0)
        {
            _currentIndex = existingIndex;
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        _items.Insert(0, song);
        _currentIndex = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddNext(Song song)
    {
        if (_items.Any(x => x.Id == song.Id)) return;

        var insertIndex = _currentIndex >= 0 ? _currentIndex + 1 : 0;

        _items.Insert(insertIndex, song);

        if (_currentIndex < 0) _currentIndex = 0;

        // In shuffle mode, prioritize the just-added song as the next track.
        if (ShuffleEnabled)
        {
            _plannedShuffleNextId = song.Id;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddToTheEnd(Song song)
    {
        if (_items.Any(x => x.Id == song.Id)) return;

        _items.Add(song);

        if (_currentIndex < 0) _currentIndex = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Song song)
    {
        var index = _items.FindIndex(x => x.Id == song.Id);

        if (index < 0) return;

        _items.RemoveAt(index);

        if (_items.Count == 0)
        {
            _currentIndex = -1;
            _shuffleHistory.Clear();
            _plannedShuffleNextId = null;
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (index < _currentIndex)
        {
            _currentIndex--;
        }
        else if (index == _currentIndex)
        {
            // Current removed; stay at same index (now points to next item)
            // or clamp to end if we were at the last position.
            if (_currentIndex >= _items.Count)
            {
                _currentIndex = _items.Count - 1;
            }
        }

        // Clean up shuffle history entries that no longer exist.
        _shuffleHistory.RemoveAll(id => _items.All(x => x.Id != id));

        // Invalidate planned next if the planned song was removed.
        if (_plannedShuffleNextId is not null &&
            _items.All(x => x.Id != _plannedShuffleNextId))
        {
            _plannedShuffleNextId = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _items.Clear();
        _currentIndex = -1;
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetContext(IEnumerable<Song> songs, Song? startSong = null)
    {
        _items.Clear();
        _items.AddRange(songs);
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;

        if (startSong is not null)
        {
            var index = _items.FindIndex(x => x.Id == startSong.Id);
            _currentIndex = index >= 0 ? index : 0;
        }
        else
        {
            _currentIndex = _items.Count > 0 ? 0 : -1;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetQueue(IEnumerable<Song> songs, int startIndex)
    {
        _items.Clear();
        _items.AddRange(songs);
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;

        _currentIndex = startIndex >= 0 && startIndex < _items.Count ? startIndex : (_items.Count > 0 ? 0 : -1);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Song? MoveNext()
    {
        if (_items.Count == 0 || _currentIndex < 0)
        {
            return null;
        }

        if (RepeatMode == RepeatMode.One)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        if (ShuffleEnabled)
        {
            return MoveNextShuffle();
        }

        if (_currentIndex < _items.Count - 1)
        {
            _currentIndex++;
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        if (RepeatMode == RepeatMode.All)
        {
            _currentIndex = 0;
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        return null;
    }

    public Song? MovePrevious()
    {
        if (_items.Count == 0 || _currentIndex < 0)
        {
            return null;
        }

        if (ShuffleEnabled && _shuffleHistory.Count > 0)
        {
            // Walk back through history skipping IDs that no longer exist.
            while (_shuffleHistory.Count > 0)
            {
                var previousId = _shuffleHistory[^1];
                _shuffleHistory.RemoveAt(_shuffleHistory.Count - 1);

                var previousIndex = _items.FindIndex(x => x.Id == previousId);
                if (previousIndex >= 0)
                {
                    _currentIndex = previousIndex;
                    Changed?.Invoke(this, EventArgs.Empty);
                    return Current;
                }
            }
        }

        if (_currentIndex > 0)
        {
            _currentIndex--;
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        if (RepeatMode == RepeatMode.All)
        {
            _currentIndex = _items.Count - 1;
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        return null;
    }

    public Song? PeekNext()
    {
        if (_items.Count == 0 || _currentIndex < 0)
        {
            return null;
        }

        if (RepeatMode == RepeatMode.One)
        {
            return Current;
        }

        if (ShuffleEnabled)
        {
            return PeekNextShuffle();
        }

        if (_currentIndex < _items.Count - 1)
        {
            return _items[_currentIndex + 1];
        }

        if (RepeatMode == RepeatMode.All)
        {
            return _items[0];
        }

        return null;
    }

    public void SetShuffle(bool enabled)
    {
        if (ShuffleEnabled == enabled)
        {
            return;
        }

        ShuffleEnabled = enabled;

        if (!ShuffleEnabled)
        {
            _shuffleHistory.Clear();
            _plannedShuffleNextId = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleShuffle()
    {
        SetShuffle(!ShuffleEnabled);
    }

    public void SetRepeatMode(RepeatMode mode)
    {
        RepeatMode = mode;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public RepeatMode CycleRepeatMode()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.Off,
            _ => RepeatMode.Off
        };

        Changed?.Invoke(this, EventArgs.Empty);
        return RepeatMode;
    }

    private Song? MoveNextShuffle()
    {
        if (_items.Count == 1)
        {
            if (RepeatMode is RepeatMode.One or RepeatMode.All)
            {
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            return null;
        }

        var currentId = Current?.Id;
        if (currentId is not null)
        {
            _shuffleHistory.Add(currentId);
        }

        if (_plannedShuffleNextId is not null)
        {
            var plannedIndex = _items.FindIndex(x => x.Id == _plannedShuffleNextId);
            if (plannedIndex >= 0 && plannedIndex != _currentIndex)
            {
                _currentIndex = plannedIndex;
                _plannedShuffleNextId = null;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }
            _plannedShuffleNextId = null;
        }

        var candidates = Enumerable
            .Range(0, _items.Count)
            .Where(x => x != _currentIndex)
            .ToList();

        _currentIndex = candidates[_random.Next(candidates.Count)];
        Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    private Song? PeekNextShuffle()
    {
        if (_items.Count == 0 || _currentIndex < 0)
        {
            return null;
        }

        if (_items.Count == 1)
        {
            return RepeatMode is RepeatMode.One or RepeatMode.All ? Current : null;
        }

        // Use planned next if available (e.g. from AddNext in shuffle mode).
        if (_plannedShuffleNextId is not null)
        {
            var plannedIndex = _items.FindIndex(x => x.Id == _plannedShuffleNextId);
            if (plannedIndex >= 0 && plannedIndex != _currentIndex)
            {
                return _items[plannedIndex];
            }
        }

        // Pure peek: pick a random candidate without storing it as planned.
        var candidates = Enumerable
            .Range(0, _items.Count)
            .Where(x => x != _currentIndex)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return _items[candidates[_random.Next(candidates.Count)]];
    }
}
