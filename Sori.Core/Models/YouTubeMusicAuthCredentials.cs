namespace Sori.Core.Models;

public sealed class YouTubeMusicAuthCredentials
{
    public string Authorization { get; init; } = "";
    public string Cookie { get; init; } = "";
    public string XGoogAuthUser { get; init; } = "0";
    public string XOrigin { get; init; } = "https://music.youtube.com";

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Authorization) &&
        !string.IsNullOrWhiteSpace(Cookie);
}
