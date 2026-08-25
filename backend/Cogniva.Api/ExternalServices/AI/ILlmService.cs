namespace Cogniva.Api.ExternalServices.AI;

public interface ILlmService
{
    Task<string> GenerateResponseAsync(
        IReadOnlyList<LlmChatMessage> messages,
        CancellationToken cancellationToken = default,
        int maxTokens = 1024,
        string? model = null);
}
