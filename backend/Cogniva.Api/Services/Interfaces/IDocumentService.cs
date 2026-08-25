using Cogniva.Api.DTOs.Documents;

namespace Cogniva.Api.Services.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItemResponse>> GetDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse> GetDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<UploadDocumentResponse> UploadDocumentAsync(
        Guid userId,
        IFormFile? file,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<DocumentDetailsResponse> SummarizeDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
