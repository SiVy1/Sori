using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Search;

public sealed class InnerTubeSearchService : ISearchService
{
    private readonly InnerTubeClient _client;
    private readonly InnerTubeContextFactory _contextFactory;
    private readonly SearchMapper _mapper;

    public InnerTubeSearchService(
        InnerTubeClient client,
        InnerTubeContextFactory contextFactory,
        SearchMapper mapper)
    {
        _client = client;
        _contextFactory = contextFactory;
        _mapper = mapper;
    }

    public async Task<SearchResponse> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "sori-search-called.txt"),
            $"Search called with query: {query}",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(query)) return new SearchResponse();

        var body = new
        {
            context = _contextFactory.CreateContext(),
            query
        };

        using var json = await _client.PostAsync(
            "search",
            body,
            cancellationToken);

        return _mapper.Map(query, json.RootElement);
    }
}