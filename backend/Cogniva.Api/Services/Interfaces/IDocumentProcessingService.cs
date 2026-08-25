using Cogniva.Api.DTOs.Documents;

namespace Cogniva.Api.Services.Interfaces;

public interface IDocumentProcessingService
{
    Task<DocumentDetailsResponse> ProcessDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
