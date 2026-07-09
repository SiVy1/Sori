using Sori.Core.Enums;

namespace Sori.Core.Models;

public sealed class SearchRequest
{
    public string Query { get; init; } = "";
    public SearchFilter Filter { get; init; } = SearchFilter.All;
    public int Limit { get; init; } = 20;
}
