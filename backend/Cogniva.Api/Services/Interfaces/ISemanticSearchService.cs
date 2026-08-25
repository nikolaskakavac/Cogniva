using Cogniva.Api.Models;

namespace Cogniva.Api.Services.Interfaces;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(
        Guid userId,
        IReadOnlyCollection<Guid> documentIds,
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}
