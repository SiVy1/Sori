using System;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Dev;

public sealed class MockUpNextService : IUpNextService
{
    public Task<UpNextResponse> GetUpNextAsync(Song song, CancellationToken cancellationToken = default)
    {
        var next = new Song
        {
            Id = "mock:upnext:1",
            SourceId = "mockvid1",
            Title = "Mock Up Next Track",
            ArtistName = "Mock Artist",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(30)
        };

        return Task.FromResult(new UpNextResponse
        {
            Current = song,
            Items = [next],
            PlaylistId = "RDAMVMmock"
        });
    }
}
