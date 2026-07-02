using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

public sealed class MockMusicClient : IMusicClient
{
    private readonly List<Song> _songs =
    [
        new()
        {
            Id = "mcr-black-parade",
            Title = "Welcome to the Black Parade",
            Artist = "My Chemical Romance",
            Album = "The Black Parade",
            Duration = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(11),
            Thumbnail = ""
        },

        new()
        {
            Id = "mcr-teenagers",
            Title = "Teenagers",
            Artist = "My Chemical Romance",
            Album = "The Black Parade",
            Duration = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(41),
            Thumbnail = ""
        },

        new()
        {
            Id = "mcr-helena",
            Title = "Helena",
            Artist = "My Chemical Romance",
            Album = "Three Cheers for Sweet Revenge",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(24),
            Thumbnail = ""
        },

        new()
        {
            Id = "paramore-misery-business",
            Title = "Misery Business",
            Artist = "Paramore",
            Album = "RIOT!",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(31),
            Thumbnail = ""
        },

        new()
        {
            Id = "paramore-crushcrushcrush",
            Title = "crushcrushcrush",
            Artist = "Paramore",
            Album = "RIOT!",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(9),
            Thumbnail = ""
        },

        new()
        {
            Id = "falloutboy-sugar",
            Title = "Sugar, We're Goin Down",
            Artist = "Fall Out Boy",
            Album = "From Under the Cork Tree",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(49),
            Thumbnail = ""
        },

        new()
        {
            Id = "falloutboy-dance-dance",
            Title = "Dance, Dance",
            Artist = "Fall Out Boy",
            Album = "From Under the Cork Tree",
            Duration = TimeSpan.FromMinutes(3),
            Thumbnail = ""
        },

        new()
        {
            Id = "patd-sins",
            Title = "I Write Sins Not Tragedies",
            Artist = "Panic! At The Disco",
            Album = "A Fever You Can't Sweat Out",
            Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(7),
            Thumbnail = ""
        }
    ];

    public Task<IReadOnlyList<Song>> SearchSongsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<Song>>(_songs);
        }

        var normalizedQuery = query.Trim();

        var results = _songs
            .Where(song =>
                song.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                song.Artist.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                song.Album.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<Song>>(results);
    }
}