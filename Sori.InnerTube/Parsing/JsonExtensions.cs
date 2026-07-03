using System.Text.Json;

namespace InnerTube.Parsing;

internal static class JsonExtensions
{
    public static bool TryGetPropertyPath(
        this JsonElement element,
        out JsonElement result,
        params string[] path)
    {
        result = element;

        foreach (var part in path)
            if (result.ValueKind != JsonValueKind.Object ||
                !result.TryGetProperty(part, out result))
                return false;

        return true;
    }

    public static string? GetStringOrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}