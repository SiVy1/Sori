using System.Text.Json;

namespace InnerTube.Search;

public static class SearchContinuationExtractor
{
    public static string? FindMusicShelfContinuationToken(JsonElement root)
    {
        foreach (var shelf in FindObjects(root, "musicShelfRenderer"))
        {
            var token = FindContinuationTokenInRenderer(shelf);
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return FindContinuationTokenInRenderer(root);
    }

    public static string? FindContinuationTokenInRenderer(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object &&
            root.ValueKind != JsonValueKind.Array)
            return null;

        // Try nextContinuationData.continuation
        foreach (var obj in FindObjects(root, "nextContinuationData"))
        {
            var continuation = FindFirstStringProperty(obj, "continuation");
            if (!string.IsNullOrWhiteSpace(continuation))
                return continuation;
        }

        // Try continuationItemRenderer.token or continuationItemRenderer.continuation
        foreach (var continuation in FindObjects(root, "continuationItemRenderer"))
        {
            var token = FindFirstStringProperty(continuation, "token");
            if (!string.IsNullOrWhiteSpace(token))
                return token;
            token = FindFirstStringProperty(continuation, "continuation");
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return null;
    }

    private static IEnumerable<JsonElement> FindObjects(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Object)
                    yield return property.Value;
                foreach (var child in FindObjects(property.Value, propertyName))
                    yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                foreach (var child in FindObjects(item, propertyName))
                    yield return child;
        }
    }

    private static string? FindFirstStringProperty(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
                var nested = FindFirstStringProperty(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var nested = FindFirstStringProperty(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }
}
