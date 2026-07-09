using System.Threading;
using System.Threading.Tasks;
using InnerTube.Browse;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace InnerTube.Home;

public sealed class InnerTubeHomeService : IHomeService
{
    private readonly InnerTubeClient _client;
    private readonly InnerTubeContextFactory _contextFactory;
    private readonly HomeMapper _mapper;

    public InnerTubeHomeService(
        InnerTubeClient client,
        InnerTubeContextFactory contextFactory,
        HomeMapper mapper)
    {
        _client = client;
        _contextFactory = contextFactory;
        _mapper = mapper;
    }

    public async Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        var body = new
        {
            context = _contextFactory.CreateContext(),
            browseId = "FEmusic_home"
        };

        using var json = await _client.PostAsync(
            "browse",
            body,
            cancellationToken);

        return _mapper.MapHome(json.RootElement);
    }
}
