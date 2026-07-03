namespace InnerTube;

public sealed class InnerTubeOptions
{
    public string BaseUrl { get; init; } = "https://music.youtube.com";
    public string ClientName { get; init; } = "WEB_REMIX";
    public string ClientVersion { get; init; } = "1.20251210.01.00";
    public string? ApiKey { get; init; } = "AIzaSyC9XL3ZjWddXya6X74dJoCTL-WEYFDNX30";
    public string H1 { get; init; } = "en";
    public string G1 { get; init; } = "PL";

    public string Origin { get; init; } = "https://music.youtube.com";
    public string Referer { get; init; } = "https://music.youtube.com";
    public string? VisitorData { get; init; }

    public string UserAgent { get; init; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";
}