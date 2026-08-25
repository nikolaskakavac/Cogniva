namespace Cogniva.Api.DTOs.Documents;

public sealed record UploadDocumentResponse(
    Guid Id,
    string Name,
    string OriginalFileName,
    string FileType,
    string Status,
    DateTimeOffset UploadedAt);
