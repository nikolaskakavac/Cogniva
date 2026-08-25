namespace Cogniva.Api.DTOs.Documents;

public sealed record DocumentDetailsResponse(
    Guid Id,
    string Name,
    string OriginalFileName,
    string FileType,
    string Status,
    string? Summary,
    string? ProcessingError,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ProcessedAt,
    int ChunkCount);
