using System;
using System.Collections.Generic;
using System.Linq;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

public sealed class QueueService : IQueueService
{
    private readonly List<Song> _userItems = [];
    private readonly List<Song> _radioItems = [];
    private readonly Random _random = new();
    private readonly List<string> _shuffleHistory = [];

    private int _userCurrentIndex = -1;
    private int _radioCurrentIndex = -1;
    private bool _isInRadio;
    private string? _plannedShuffleNextId;

    public IReadOnlyList<Song> Items => _userItems.Concat(_radioItems).ToList();

    public int CurrentIndex
    {
        get
        {
            if (_isInRadio && _radioCurrentIndex >= 0)
                return _userItems.Count + _radioCurrentIndex;
            return _userCurrentIndex;
        }
    }

    public Song? Current
    {
        get
        {
            if (_isInRadio)
                return _radioCurrentIndex >= 0 && _radioCurrentIndex < _radioItems.Count
                    ? _radioItems[_radioCurrentIndex]
                    : null;
            return _userCurrentIndex >= 0 && _userCurrentIndex < _userItems.Count
                ? _userItems[_userCurrentIndex]
                : null;
        }
    }

    public bool ShuffleEnabled { get; private set; }

    public RepeatMode RepeatMode { get; private set; } = RepeatMode.Off;

    public int UserItemCount => _userItems.Count;

    public bool RadioEnabled { get; private set; } = true;

    public event EventHandler? Changed;

    public void PlayNow(Song song)
    {
        _userItems.Insert(0, song);
        _userCurrentIndex = 0;
        _isInRadio = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddNext(Song song)
    {
        int insertIndex;
        if (_isInRadio)
        {
            // Current is in radio; insert at end of user items (right before radio).
            insertIndex = _userItems.Count;
        }
        else
        {
            insertIndex = _userCurrentIndex >= 0 ? _userCurrentIndex + 1 : 0;
        }

        _userItems.Insert(insertIndex, song);

        if (_userCurrentIndex < 0) _userCurrentIndex = 0;

        // In shuffle mode, prioritize the just-added song as the next track.
        if (ShuffleEnabled)
        {
            _plannedShuffleNextId = song.Id;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddToTheEnd(Song song)
    {
        _userItems.Add(song);

        if (_userCurrentIndex < 0) _userCurrentIndex = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Song song)
    {
        var userIndex = _userItems.FindIndex(x => x.Id == song.Id);
        if (userIndex >= 0)
        {
            _userItems.RemoveAt(userIndex);
            _shuffleHistory.RemoveAll(id => _userItems.All(x => x.Id != id));
            if (_plannedShuffleNextId is not null &&
                _userItems.All(x => x.Id != _plannedShuffleNextId) &&
                _radioItems.All(x => x.Id != _plannedShuffleNextId))
            {
                _plannedShuffleNextId = null;
            }

            if (!_isInRadio)
            {
                if (userIndex < _userCurrentIndex)
                {
                    _userCurrentIndex--;
                }
                else if (userIndex == _userCurrentIndex)
                {
                    // Current removed; stay at same index or clamp to end.
                    if (_userCurrentIndex >= _userItems.Count)
                    {
                        if (_radioItems.Count > 0 && RadioEnabled)
                        {
                            _isInRadio = true;
                            _radioCurrentIndex = 0;
                        }
                        else
                        {
                            _userCurrentIndex = _userItems.Count - 1;
                        }
                    }
                }
            }
            else
            {
                // Current is in radio, just adjust user index for back-navigation.
                if (userIndex < _userCurrentIndex)
                {
                    _userCurrentIndex--;
                }
            }

            if (_userItems.Count == 0 && _radioItems.Count == 0)
            {
                _userCurrentIndex = -1;
                _radioCurrentIndex = -1;
                _isInRadio = false;
                _shuffleHistory.Clear();
                _plannedShuffleNextId = null;
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        var radioIndex = _radioItems.FindIndex(x => x.Id == song.Id);
        if (radioIndex >= 0)
        {
            _radioItems.RemoveAt(radioIndex);

            if (_isInRadio)
            {
                if (radioIndex < _radioCurrentIndex)
                {
                    _radioCurrentIndex--;
                }
                else if (radioIndex == _radioCurrentIndex)
                {
                    // Current radio item removed; check boundaries.
                    if (_radioCurrentIndex >= _radioItems.Count)
                    {
                        if (_radioItems.Count > 0)
                        {
                            _radioCurrentIndex = _radioItems.Count - 1;
                        }
                        else
                        {
                            // No radio items left; fall back to user items.
                            _isInRadio = false;
                            _radioCurrentIndex = -1;
                            if (_userItems.Count > 0)
                                _userCurrentIndex = Math.Min(_userCurrentIndex, _userItems.Count - 1);
                            else
                                _userCurrentIndex = -1;
                        }
                    }
                }
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        _userItems.Clear();
        _radioItems.Clear();
        _userCurrentIndex = -1;
        _radioCurrentIndex = -1;
        _isInRadio = false;
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetContext(IEnumerable<Song> songs, Song? startSong = null)
    {
        _userItems.Clear();
        _userItems.AddRange(songs);
        _radioItems.Clear();
        _radioCurrentIndex = -1;
        _isInRadio = false;
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;

        if (startSong is not null)
        {
            var index = _userItems.FindIndex(x => x.Id == startSong.Id);
            _userCurrentIndex = index >= 0 ? index : 0;
        }
        else
        {
            _userCurrentIndex = _userItems.Count > 0 ? 0 : -1;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetQueue(IEnumerable<Song> songs, int startIndex)
    {
        _userItems.Clear();
        _userItems.AddRange(songs);
        _radioItems.Clear();
        _radioCurrentIndex = -1;
        _isInRadio = false;
        _shuffleHistory.Clear();
        _plannedShuffleNextId = null;

        _userCurrentIndex = startIndex >= 0 && startIndex < _userItems.Count ? startIndex : (_userItems.Count > 0 ? 0 : -1);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetRadioQueue(IEnumerable<Song> songs)
    {
        _radioItems.Clear();

        if (_isInRadio)
        {
            // Current was in radio; fall back to user items.
            _isInRadio = false;
            _radioCurrentIndex = -1;
            if (_userItems.Count > 0)
                _userCurrentIndex = Math.Min(_userCurrentIndex, _userItems.Count - 1);
            else
                _userCurrentIndex = -1;
        }

        if (RadioEnabled)
        {
            _radioItems.AddRange(songs);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleRadio()
    {
        RadioEnabled = !RadioEnabled;

        if (!RadioEnabled)
        {
            _radioItems.Clear();
            if (_isInRadio)
            {
                _isInRadio = false;
                _radioCurrentIndex = -1;
                if (_userItems.Count > 0)
                    _userCurrentIndex = Math.Min(_userCurrentIndex, _userItems.Count - 1);
                else
                    _userCurrentIndex = -1;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Song? MoveNext()
    {
        if (Current is null)
        {
            return null;
        }

        if (RepeatMode == RepeatMode.One)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return Current;
        }

        if (ShuffleEnabled && !_isInRadio)
        {
            return MoveNextShuffle();
        }

        if (!_isInRadio)
        {
            // In user items.
            if (_userCurrentIndex < _userItems.Count - 1)
            {
                _userCurrentIndex++;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            // At end of user items; try radio.
            if (_radioItems.Count > 0 && RadioEnabled)
            {
                _isInRadio = true;
                _radioCurrentIndex = 0;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            if (RepeatMode == RepeatMode.All)
            {
                _userCurrentIndex = 0;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            return null;
        }
        else
        {
            // In radio items.
            if (_radioCurrentIndex < _radioItems.Count - 1)
            {
                _radioCurrentIndex++;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            if (RepeatMode == RepeatMode.All)
            {
                _radioCurrentIndex = 0;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            return null;
        }
    }

    public Song? MovePrevious()
    {
        if (Current is null)
        {
            return null;
        }

        if (ShuffleEnabled && !_isInRadio && _shuffleHistory.Count > 0)
        {
            // Walk back through history skipping IDs that no longer exist.
            while (_shuffleHistory.Count > 0)
            {
                var previousId = _shuffleHistory[^1];
                _shuffleHistory.RemoveAt(_shuffleHistory.Count - 1);

                var previousIndex = _userItems.FindIndex(x => x.Id == previousId);
                if (previousIndex >= 0)
                {
                    _userCurrentIndex = previousIndex;
                    _isInRadio = false;
                    Changed?.Invoke(this, EventArgs.Empty);
                    return Current;
                }
            }
        }

        if (_isInRadio)
        {
            // In radio items.
            if (_radioCurrentIndex > 0)
            {
                _radioCurrentIndex--;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            // Go back to last user item.
            if (_userItems.Count > 0)
            {
                _isInRadio = false;
                _userCurrentIndex = _userItems.Count - 1;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            if (RepeatMode == RepeatMode.All)
            {
                _radioCurrentIndex = _radioItems.Count - 1;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            return null;
        }
        else
        {
            // In user items.
            if (_userCurrentIndex > 0)
            {
                _userCurrentIndex--;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            if (RepeatMode == RepeatMode.All)
            {
                _userCurrentIndex = _userItems.Count > 0 ? _userItems.Count - 1 : -1;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            return null;
        }
    }

    public Song? PeekNext()
    {
        if (Current is null) return null;

        if (RepeatMode == RepeatMode.One)
        {
            return Current;
        }

        if (ShuffleEnabled && !_isInRadio)
        {
            return PeekNextShuffle();
        }

        if (!_isInRadio)
        {
            if (_userCurrentIndex < _userItems.Count - 1)
            {
                return _userItems[_userCurrentIndex + 1];
            }

            if (_radioItems.Count > 0 && RadioEnabled)
            {
                return _radioItems[0];
            }

            if (RepeatMode == RepeatMode.All)
            {
                return _userItems.Count > 0 ? _userItems[0] : null;
            }

            return null;
        }
        else
        {
            if (_radioCurrentIndex < _radioItems.Count - 1)
            {
                return _radioItems[_radioCurrentIndex + 1];
            }

            if (RepeatMode == RepeatMode.All)
            {
                return _radioItems.Count > 0 ? _radioItems[0] : null;
            }

            return null;
        }
    }

    public Song? PeekPlannedNext()
    {
        if (Current is null) return null;

        if (RepeatMode == RepeatMode.One)
        {
            return Current;
        }

        if (ShuffleEnabled && !_isInRadio)
        {
            return PeekPlannedNextShuffle();
        }

        if (!_isInRadio)
        {
            if (_userCurrentIndex < _userItems.Count - 1)
            {
                return _userItems[_userCurrentIndex + 1];
            }

            if (_radioItems.Count > 0 && RadioEnabled)
            {
                return _radioItems[0];
            }

            if (RepeatMode == RepeatMode.All)
            {
                return _userItems.Count > 0 ? _userItems[0] : null;
            }

            return null;
        }
        else
        {
            if (_radioCurrentIndex < _radioItems.Count - 1)
            {
                return _radioItems[_radioCurrentIndex + 1];
            }

            if (RepeatMode == RepeatMode.All)
            {
                return _radioItems.Count > 0 ? _radioItems[0] : null;
            }

            return null;
        }
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
        // Only operates on user items; radio stays linear.
        if (_userItems.Count == 0)
        {
            return null;
        }

        if (_userItems.Count == 1)
        {
            if (RepeatMode is RepeatMode.One or RepeatMode.All)
            {
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }

            // Only one user item; try radio.
            if (_radioItems.Count > 0 && RadioEnabled)
            {
                _isInRadio = true;
                _radioCurrentIndex = 0;
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
            var plannedIndex = _userItems.FindIndex(x => x.Id == _plannedShuffleNextId);
            if (plannedIndex >= 0 && plannedIndex != _userCurrentIndex)
            {
                _userCurrentIndex = plannedIndex;
                _plannedShuffleNextId = null;
                Changed?.Invoke(this, EventArgs.Empty);
                return Current;
            }
            _plannedShuffleNextId = null;
        }

        var candidates = Enumerable
            .Range(0, _userItems.Count)
            .Where(x => x != _userCurrentIndex)
            .ToList();

        _userCurrentIndex = candidates[_random.Next(candidates.Count)];
        Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    private Song? PeekNextShuffle()
    {
        if (_userItems.Count == 0 || _userCurrentIndex < 0)
        {
            return null;
        }

        if (_userItems.Count == 1)
        {
            return RepeatMode is RepeatMode.One or RepeatMode.All ? Current : null;
        }

        if (_plannedShuffleNextId is not null)
        {
            var plannedIndex = _userItems.FindIndex(x => x.Id == _plannedShuffleNextId);
            if (plannedIndex >= 0 && plannedIndex != _userCurrentIndex)
            {
                return _userItems[plannedIndex];
            }
            _plannedShuffleNextId = null;
        }

        var candidates = Enumerable
            .Range(0, _userItems.Count)
            .Where(x => x != _userCurrentIndex)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return _userItems[candidates[_random.Next(candidates.Count)]];
    }

    private Song? PeekPlannedNextShuffle()
    {
        if (_userItems.Count == 0 || _userCurrentIndex < 0)
        {
            return null;
        }

        if (_userItems.Count == 1)
        {
            return RepeatMode is RepeatMode.One or RepeatMode.All ? Current : null;
        }

        if (_plannedShuffleNextId is not null)
        {
            var plannedIndex = _userItems.FindIndex(x => x.Id == _plannedShuffleNextId);
            if (plannedIndex >= 0 && plannedIndex != _userCurrentIndex)
            {
                return _userItems[plannedIndex];
            }
            _plannedShuffleNextId = null;
        }

        var candidates = Enumerable
            .Range(0, _userItems.Count)
            .Where(x => x != _userCurrentIndex)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var nextIndex = candidates[_random.Next(candidates.Count)];
        _plannedShuffleNextId = _userItems[nextIndex].Id;
        return _userItems[nextIndex];
    }
}
