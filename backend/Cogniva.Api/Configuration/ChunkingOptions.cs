using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.Configuration;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    [Range(50, 4000)] public int TargetTokens { get; init; } = 750;
    [Range(0, 1000)] public int OverlapTokens { get; init; } = 120;
    [Range(1, 1000)] public int MinimumTokens { get; init; } = 80;
}
