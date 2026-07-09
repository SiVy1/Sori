using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IPlaybackResolver
{
    Task<PlayableTrack> ResolveAsync(
        Song song,
        CancellationToken cancellationToken = default);
}
