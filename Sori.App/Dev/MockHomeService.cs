using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Dev;

public sealed class MockHomeService : IHomeService
{
    public Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HomeResponse
        {
            Sections =
            [
                new HomeSection
                {
                    Title = "Quick picks",
                    Items =
                    [
                        new HomeItem
                        {
                            Kind = HomeItemKind.Song,
                            Song = new Song
                            {
                                Id = "mock:song:1",
                                Title = "Mock Song",
                                ArtistName = "Mock Artist"
                            }
                        }
                    ]
                }
            ]
        });
    }
}
