using System.Text.Json;
using InnerTube.Browse;
using Sori.Core.Models;

var json = @""
{
    ""header"": {
        ""musicImmersiveHeaderRenderer"": {
            ""title"": { ""runs"": [{ ""text"": ""Test Artist"" }] },
            ""description"": { ""runs"": [{ ""text"": ""1.2M subscribers"" }] }
        }
    },
    ""contents"": {
        ""singleColumnBrowseResultsRenderer"": { ""tabs"": [] }
    }
}
"";

using var doc = JsonDocument.Parse(json);

// Direct test: FindObjects
var results = FindObjects(doc.RootElement, ""musicImmersiveHeaderRenderer"").ToList();
Console.WriteLine($""Found {results.Count} header renderers"");
foreach (var r in results)
{
    Console.WriteLine($""  ValueKind: {r.ValueKind}"");
    if (r.TryGetProperty(""title"", out var title))
    {
        Console.WriteLine($""  Has title: {title}"");
    }
}

var mapper = new BrowseMapper();
var artist = new Artist { Id = ""test"", SourceId = ""UCtest"", Name = ""Fallback"" };
var detail = mapper.MapArtist(artist, doc.RootElement);
Console.WriteLine($""Name: '{detail.Name}'"");
Console.WriteLine($""Subtitle: '{detail.Subtitle}'"");

// Copy FindObjects inline for debugging
static IEnumerable<JsonElement> FindObjects(JsonElement root, string propertyName)
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
