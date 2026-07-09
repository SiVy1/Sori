using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

public sealed class MockYouTubeMusicAuthStore : IYouTubeMusicAuthStore
{
    private YouTubeMusicAuthCredentials? _credentials;

    public Task<YouTubeMusicAuthCredentials?> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_credentials);

    public Task SaveAsync(YouTubeMusicAuthCredentials credentials, CancellationToken cancellationToken = default)
    {
        _credentials = credentials;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _credentials = null;
        return Task.CompletedTask;
    }
}
