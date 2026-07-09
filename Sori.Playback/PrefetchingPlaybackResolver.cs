using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace Sori.Playback;

public sealed class PrefetchingPlaybackResolver : IPrefetchingPlaybackResolver
{
    private readonly IPlaybackResolver _inner;
    private readonly object _gate = new();
    private readonly Dictionary<string, PlayableTrack> _cache = new();
    private readonly Dictionary<string, Task<PlayableTrack>> _inFlight = new();

    private const int MaxCacheSize = 3;

    public PrefetchingPlaybackResolver(IPlaybackResolver inner)
    {
        _inner = inner;
    }

    public Task<PlayableTrack> ResolveAsync(
        Song song,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(song);
        Task<PlayableTrack>? inFlight = null;

        lock (_gate)
        {
            if (_cache.Remove(key, out var cached))
            {
                return Task.FromResult(cached);
            }

            _inFlight.TryGetValue(key, out inFlight);
        }

        if (inFlight is not null)
        {
            return AwaitExistingAsync(inFlight, key, cancellationToken);
        }

        return _inner.ResolveAsync(song, cancellationToken);
    }

    public Task PrefetchAsync(
        Song song,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(song);

        lock (_gate)
        {
            if (_cache.ContainsKey(key) || _inFlight.ContainsKey(key))
            {
                return Task.CompletedTask;
            }

            var task = PrefetchCoreAsync(song, key, cancellationToken);
            _inFlight[key] = task;
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _cache.Clear();
            _inFlight.Clear();
        }
    }

    private async Task<PlayableTrack> PrefetchCoreAsync(
        Song song,
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            var playable = await _inner.ResolveAsync(song, cancellationToken);

            lock (_gate)
            {
                _cache[key] = playable;

                while (_cache.Count > MaxCacheSize)
                {
                    var firstKey = _cache.Keys.First();
                    _cache.Remove(firstKey);
                }
            }

            return playable;
        }
        finally
        {
            lock (_gate)
            {
                _inFlight.Remove(key);
            }
        }
    }

    private async Task<PlayableTrack> AwaitExistingAsync(
        Task<PlayableTrack> existingTask,
        string key,
        CancellationToken cancellationToken)
    {
        var playable = await existingTask.WaitAsync(cancellationToken);

        lock (_gate)
        {
            _cache.Remove(key);
        }

        return playable;
    }

    private static string GetKey(Song song)
    {
        return song.Id;
    }
}
