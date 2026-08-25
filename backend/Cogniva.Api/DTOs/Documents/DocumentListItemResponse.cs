namespace Cogniva.Api.DTOs.Documents;

public sealed record DocumentListItemResponse(
    Guid Id,
    string Name,
    string OriginalFileName,
    string FileType,
    string Status,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ProcessedAt);
