using Cogniva.Api.Models.Processing;

namespace Cogniva.Api.Services.Interfaces;

public interface ITextExtractionService
{
    Task<ExtractedDocument> ExtractAsync(
        string physicalPath,
        string fileType,
        CancellationToken cancellationToken = default);
}
