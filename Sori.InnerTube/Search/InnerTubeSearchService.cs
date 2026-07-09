using Sori.Core.Enums;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Search;

public sealed class InnerTubeSearchService : ISearchService
{
    private readonly InnerTubeClient _client;
    private readonly InnerTubeContextFactory _contextFactory;
    private readonly SearchMapper _mapper;

    // ytmusicapi filter params for WEB_REMIX
    private static readonly Dictionary<SearchFilter, string> FilterParams = new()
    {
        { SearchFilter.Songs, "EgWKAQIIAWoKEAkQAxAFEAoQBA%3D%3D" },
        { SearchFilter.Albums, "EgWKAQIYAWoKEAkQAxAFEAoQBA%3D%3D" },
        { SearchFilter.Artists, "EgWKAQIgAWoKEAkQAxAFEAoQBA%3D%3D" },
        { SearchFilter.Playlists, "EgWKAQIoAWoKEAkQAxAFEAoQBA%3D%3D" },
    };

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
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SearchResponse();

        var body = BuildSearchBody(request);

        using var firstJson = await _client.PostAsync(
            "search",
            body,
            cancellationToken);

        var result = _mapper.Map(request.Query, firstJson.RootElement);

        // No continuation for unfiltered search
        if (request.Filter == SearchFilter.All)
            return result;

        // Fetch continuations until limit reached
        var token = SearchContinuationExtractor
            .FindMusicShelfContinuationToken(firstJson.RootElement);

        var guard = 0;

        while (!string.IsNullOrWhiteSpace(token) &&
               SearchMapper.CountForFilter(result, request.Filter) < request.Limit &&
               guard < 10)
        {
            guard++;

            var continuationQuery = BuildContinuationQuery(token);

            using var continuationJson = await _client.PostAsync(
                "search",
                body,
                continuationQuery,
                cancellationToken);

            var continuationResult = _mapper.MapContinuation(
                request.Query,
                continuationJson.RootElement);

            result = SearchMapper.MergeForFilter(
                result, continuationResult, request.Filter, request.Limit);

            token = SearchContinuationExtractor
                .FindMusicShelfContinuationToken(continuationJson.RootElement);
        }

        return result;
    }

    private object BuildSearchBody(SearchRequest request)
    {
        var body = new Dictionary<string, object?>
        {
            ["context"] = _contextFactory.CreateContext(),
            ["query"] = request.Query
        };

        if (request.Filter != SearchFilter.All &&
            FilterParams.TryGetValue(request.Filter, out var param))
        {
            body["params"] = param;
        }

        return body;
    }

    private static string BuildContinuationQuery(string token)
    {
        var escaped = Uri.EscapeDataString(token);
        return $"ctoken={escaped}&continuation={escaped}";
    }
}
