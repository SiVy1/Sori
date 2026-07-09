using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IPrefetchingPlaybackResolver : IPlaybackResolver
{
    Task PrefetchAsync(
        Song song,
        CancellationToken cancellationToken = default);

    void Clear();
}
