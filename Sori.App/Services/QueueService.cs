using System.Collections.Generic;
using System.Linq;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

public sealed class QueueService : IQueueService
{
    private readonly List<Song> _items = [];
    private int _currentIndex = -1;

    public IReadOnlyList<Song> Items => _items;
    
    public Song? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    public void PlayNow(Song song)
    {
        var existingIndex = _items.FindIndex(x => x.Id == song.Id);

        if (existingIndex >= 0)
        {
            _currentIndex = existingIndex;
            return;
        }
        
        _items.Insert(0, song);
        _currentIndex = 0;
    }

    public void AddNext(Song song)
    {
        if (_items.Any(x => x.Id == song.Id))
        {
            return;
        }
        
        var insertIndex = _currentIndex >= 0 ? _currentIndex + 1: 0;
        
        _items.Insert(insertIndex, song);

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }
    }

    public void AddToTheEnd(Song song)
    {
        if (_items.Any(x => x.Id == song.Id))
        {
            return;
        }
        
        _items.Add(song);

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }
    }

    public void Remove(Song song)
    {
        var index = _items.FindIndex(x => x.Id == song.Id);

        if (index < 0)
        {
            return;
        }
        
        _items.RemoveAt(index);

        if (_items.Count == 0)
        {
            _currentIndex = -1;
            return;
        }

        if (_currentIndex >= _items.Count)
        {
            _currentIndex = _items.Count - 1;
        }
    }

    public void Clear()
    {
        _items.Clear();
        _currentIndex = -1;
    }
    
    public Song? MoveNext()
    {
        if (_items.Count == 0)
        {
            return null;
        }

        if (_currentIndex < _items.Count - 1)
        {
            _currentIndex++;
        }

        return Current;
    }

    public Song? MovePrevious()
    {
        if (_items.Count == 0)
        {
            return null;
        }

        if (_currentIndex > 0)
        {
            _currentIndex--;
        }

        return Current;
    }
}