using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IYouTubeMusicAuthStore
{
    Task<YouTubeMusicAuthCredentials?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(YouTubeMusicAuthCredentials credentials, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
