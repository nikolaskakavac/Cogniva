namespace Cogniva.Api.Models;

public sealed record SemanticSearchResult(
    Guid DocumentChunkId,
    Guid DocumentId,
    string DocumentName,
    string Content,
    int ChunkIndex,
    int? PageNumber,
    double CosineDistance)
{
    public double RelevanceScore => Math.Clamp(1 - CosineDistance, 0, 1);
}
