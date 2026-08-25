using Cogniva.Api.ExternalServices.AI;
using Cogniva.Api.Models;

namespace Cogniva.Api.Services.Interfaces;

public interface IRagService
{
    Task<RagResult> AnswerAsync(
        Guid userId,
        IReadOnlyCollection<Guid> documentIds,
        string question,
        IReadOnlyList<LlmChatMessage> history,
        CancellationToken cancellationToken = default);
}
