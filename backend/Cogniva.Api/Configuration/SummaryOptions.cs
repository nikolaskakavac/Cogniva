using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.Configuration;

public sealed class SummaryOptions
{
    public const string SectionName = "Summary";

    [Range(1000, 60000)] public int DirectSummaryMaxCharacters { get; init; } = 16000;
    [Range(1000, 60000)] public int PartialBatchMaxCharacters { get; init; } = 12000;
    [Range(2, 20)] public int MaxPartialSummariesPerFinalPrompt { get; init; } = 8;
    [Range(100, 1000)] public int PartialMaxTokens { get; init; } = 350;
    [Range(200, 2000)] public int FinalMaxTokens { get; init; } = 700;
}
