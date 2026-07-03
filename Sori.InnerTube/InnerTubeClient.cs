using System.Net.Http.Json;
using System.Text.Json;

namespace InnerTube;

public sealed class InnerTubeClient
{
    private readonly HttpClient _httpClient;
    private readonly InnerTubeOptions _options;

    public InnerTubeClient(HttpClient httpClient, InnerTubeOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<JsonDocument> PostAsync(
        string endpoint,
        object body,
        CancellationToken cancellationToken = default)
    {
        var debugDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "sori-debug");

        Directory.CreateDirectory(debugDir);

        await File.WriteAllTextAsync(
            Path.Combine(debugDir, "01-before-request.txt"),
            $"Endpoint: {endpoint}\nUrl: {BuildUrl(endpoint)}",
            cancellationToken);

        var url = BuildUrl(endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Origin", _options.Origin);
        request.Headers.TryAddWithoutValidation("Referer", _options.Referer);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.TryAddWithoutValidation("X-Youtube-Client-Name", "67");
        request.Headers.TryAddWithoutValidation("X-Youtube-Client-Version", _options.ClientVersion);
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Format-Version", "2");

        if (!string.IsNullOrWhiteSpace(_options.VisitorData))
            request.Headers.TryAddWithoutValidation("X-Goog-Visitor-Id", _options.VisitorData);

        request.Content = JsonContent.Create(body);

        await File.WriteAllTextAsync(
            Path.Combine(debugDir, "02-before-send.txt"),
            "About to send request",
            cancellationToken);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(debugDir, "03-after-send.txt"),
            $"Status: {(int)response.StatusCode} {response.ReasonPhrase}",
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(debugDir, "04-response.json"),
            responseText,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"InnerTube request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");

        return JsonDocument.Parse(responseText);
    }

    private string BuildUrl(string endpoint)
    {
        var url =
            $"{_options.BaseUrl.TrimEnd('/')}/youtubei/v1/{endpoint.TrimStart('/')}?prettyPrint=false";

        if (!string.IsNullOrWhiteSpace(_options.ApiKey)) url += $"&key={Uri.EscapeDataString(_options.ApiKey)}";

        return url;
    }
}