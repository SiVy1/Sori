using System.Text.Json;
using Sori.Core.Models;

namespace InnerTube.Auth;

public static class BrowserHeadersParser
{
    public static YouTubeMusicAuthCredentials Parse(string rawHeaders)
    {
        if (string.IsNullOrWhiteSpace(rawHeaders))
            throw new InvalidOperationException("Headers are empty.");

        var headers = ParseJson(rawHeaders) ?? ParseCurl(rawHeaders) ?? ParseRaw(rawHeaders);

        var authorization = GetRequired(headers, "Authorization");
        var cookie = GetRequired(headers, "Cookie");

        headers.TryGetValue("X-Goog-AuthUser", out var authUser);
        headers.TryGetValue("x-goog-authuser", out var authUserLower);
        var xGoogAuthUser = authUser ?? authUserLower ?? "0";

        headers.TryGetValue("x-origin", out var xOrigin);
        var origin = xOrigin ?? "https://music.youtube.com";

        var credentials = new YouTubeMusicAuthCredentials
        {
            Authorization = authorization,
            Cookie = cookie,
            XGoogAuthUser = xGoogAuthUser,
            XOrigin = origin
        };

        if (!credentials.IsValid)
            throw new InvalidOperationException(
                "Missing Authorization or Cookie. Open DevTools → Network → find a /browse POST request → " +
                "in Chrome use 'Copy as cURL' then extract -H 'cookie: ...' line. " +
                "Or right-click the request → Copy → Copy as fetch (Node.js).");

        return credentials;
    }

    private static Dictionary<string, string>? ParseJson(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith('{')) return null;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Chrome "Copy" on Headers tab gives {"headers": {"key": "value"}}
            if (root.TryGetProperty("headers", out var headersObj) &&
                headersObj.ValueKind == JsonValueKind.Object)
                root = headersObj;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    result[prop.Name] = prop.Value.GetString()!;
                else if (prop.Value.ValueKind == JsonValueKind.Number)
                    result[prop.Name] = prop.Value.ToString()!;
            }
            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string>? ParseCurl(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith("curl", StringComparison.OrdinalIgnoreCase)) return null;

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Match -H 'key: value' or -H "key: value"
        var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"-H\s+['""]([^'""]+)['""]");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var header = match.Groups[1].Value;
            var separatorIndex = header.IndexOf(':');
            if (separatorIndex <= 0) continue;
            var name = header[..separatorIndex].Trim();
            var value = header[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                headers[name] = value;
        }

        // Also try --header 'key: value' (long form)
        var longMatches = System.Text.RegularExpressions.Regex.Matches(raw, @"--header\s+['""]([^'""]+)['""]");
        foreach (System.Text.RegularExpressions.Match match in longMatches)
        {
            var header = match.Groups[1].Value;
            var separatorIndex = header.IndexOf(':');
            if (separatorIndex <= 0) continue;
            var name = header[..separatorIndex].Trim();
            var value = header[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                headers[name] = value;
        }

        return headers.Count > 0 ? headers : null;
    }

    private static Dictionary<string, string> ParseRaw(string raw)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in raw.Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0) continue;
            var name = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                headers[name] = value;
        }
        return headers;
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> headers, string name)
    {
        if (!headers.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Missing required header: {name}. " +
                "Make sure to include the Cookie header. " +
                "In Chrome DevTools, use the 'Copy as cURL' option on a /browse POST request, " +
                "then look for -H 'cookie: ...' in the output.");
        return value;
    }
}
