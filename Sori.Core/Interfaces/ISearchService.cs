using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}