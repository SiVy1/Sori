using Sori.Core.Models;

namespace Sori.Core.Interfaces;

public interface IMusicClient
{
    public Task<IReadOnlyList<Song>> SearchSongsAsync(string query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}