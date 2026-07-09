using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IHomeService
{
    Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default);
}
