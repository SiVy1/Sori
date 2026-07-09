using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sori.Core.Interfaces;
using Sori.Core.Models;

namespace App.Services;

// ponytail: Simple Base64-encoded file store. DPAPI (System.Security.Cryptography.ProtectedData)
// NuGet isn't available for .NET 10 preview yet. Switch to DPAPI when the package ships.
public sealed class DpapiYouTubeMusicAuthStore : IYouTubeMusicAuthStore
{
    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sori",
            "auth.dat");

    public Task<YouTubeMusicAuthCredentials?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
            return Task.FromResult<YouTubeMusicAuthCredentials?>(null);

        try
        {
            var encoded = File.ReadAllText(FilePath, Encoding.UTF8);
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var credentials = JsonSerializer.Deserialize<YouTubeMusicAuthCredentials>(json);
            return Task.FromResult(credentials);
        }
        catch
        {
            return Task.FromResult<YouTubeMusicAuthCredentials?>(null);
        }
    }

    public Task SaveAsync(YouTubeMusicAuthCredentials credentials, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(credentials);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        File.WriteAllText(FilePath, encoded, Encoding.UTF8);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        return Task.CompletedTask;
    }
}
