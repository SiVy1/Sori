using System.Text.Json;
using Xunit;

namespace DebugTest;

public class DebugFindObjects
{
    private static IEnumerable<JsonElement> FindObjects(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Object)
                    yield return property.Value;
                foreach (var child in FindObjects(property.Value, propertyName)) yield return child;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                foreach (var child in FindObjects(item, propertyName)) yield return child;
        }
    }

    [Fact]
    public void FindHeader_Works()
    {
        var json = @"{
    ""header"": {
        ""musicImmersiveHeaderRenderer"": {
            ""title"": { ""runs"": [{ ""text"": ""Test Artist"" }] }
        }
    },
    ""contents"": {}
}";
        using var doc = JsonDocument.Parse(json);
        var results = FindObjects(doc.RootElement, ""musicImmersiveHeaderRenderer"").ToList();
        Assert.Single(results);
        var header = results[0];
        Assert.True(header.TryGetProperty(""title"", out var title));
        Assert.Equal(""Test Artist"", title.GetProperty(""runs"")[0].GetProperty(""text"").GetString());
    }
}
