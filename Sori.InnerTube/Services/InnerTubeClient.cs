using System;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Services;

public class InnerTubeClient : ISearchService
{
    public Task<SearchResponse> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
