using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Dev;

public sealed class MockMusicClient : ISearchService
{
    private readonly List<Song> _songs =
    [
        new()
        {
            Id = "mcr-black-parade",
            Title = "Welcome to the Black Parade",
            ArtistName = "My Chemical Romance",
            AlbumTitle = "The Black Parade",
            Duration = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(11),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "mcr-teenagers",
            Title = "Teenagers",
            ArtistName = "My Chemical Romance",
            AlbumTitle = "The Black Parade",
            Duration = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(41),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "mcr-helena",
            Title = "Helena",
            ArtistName = "My Chemical Romance",
            AlbumTitle = "Three Cheers for Sweet Revenge",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(24),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "paramore-misery-business",
            Title = "Misery Business",
            ArtistName = "Paramore",
            AlbumTitle = "RIOT!",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(31),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "paramore-crushcrushcrush",
            Title = "crushcrushcrush",
            ArtistName = "Paramore",
            AlbumTitle = "RIOT!",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(9),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "falloutboy-sugar",
            Title = "Sugar, We're Goin Down",
            ArtistName = "Fall Out Boy",
            AlbumTitle = "From Under the Cork Tree",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(49),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "falloutboy-dance-dance",
            Title = "Dance, Dance",
            ArtistName = "Fall Out Boy",
            AlbumTitle = "From Under the Cork Tree",
            Duration = TimeSpan.FromMinutes(3),
            ThumbnailUrl = null
        },

        new()
        {
            Id = "patd-sins",
            Title = "I Write Sins Not Tragedies",
            ArtistName = "Panic! At The Disco",
            AlbumTitle = "A Fever You Can't Sweat Out",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(7),
            ThumbnailUrl = null
        }
    ];

    public async Task<SearchResponse> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        if (query.Equals("empty", StringComparison.OrdinalIgnoreCase)) return new SearchResponse();

        if (query.Equals("error", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mock search error.");

        if (string.IsNullOrWhiteSpace(query)) return new SearchResponse { Songs = _songs };

        var normalizedQuery = query.Trim();

        var results = _songs
            .Where(song =>
                song.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                song.ArtistName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                (song.AlbumTitle?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        return new SearchResponse { Songs = results };
    }
}