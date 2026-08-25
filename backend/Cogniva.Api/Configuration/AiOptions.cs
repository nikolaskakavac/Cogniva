using System.ComponentModel.DataAnnotations;

namespace Cogniva.Api.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; init; } = "OpenAICompatible";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string ApiKey { get; init; } = string.Empty;
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    [Range(1, 10000)] public int EmbeddingDimensions { get; init; } = 1536;
}
