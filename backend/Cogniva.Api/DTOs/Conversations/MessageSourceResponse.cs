namespace Cogniva.Api.DTOs.Conversations;

public sealed record MessageSourceResponse(
    Guid DocumentId,
    Guid DocumentChunkId,
    string DocumentName,
    int ChunkIndex,
    int? PageNumber,
    string Snippet,
    double RelevanceScore);
