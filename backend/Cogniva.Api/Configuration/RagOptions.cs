using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "RAG";

    [Range(1, 20)] public int TopK { get; init; } = 5;
    [Range(0, 2)] public double MaxCosineDistance { get; init; } = 0.65;
    [Range(0, 20)] public int HistoryMessageLimit { get; init; } = 8;
    [Range(100, 20000)] public int MaxContextCharacters { get; init; } = 12000;
}
