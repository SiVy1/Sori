using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IUpNextService
{
    Task<UpNextResponse> GetUpNextAsync(
        Song song,
        CancellationToken cancellationToken = default);
}
