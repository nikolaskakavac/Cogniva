using Cogniva.Api.DTOs.Conversations;

namespace Cogniva.Api.Services.Interfaces;

public interface IConversationService
{
    Task<ConversationDetailsResponse> CreateAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationListItemResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ConversationDetailsResponse> GetAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<MessageResponse> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> RetryMessageAsync(Guid userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
}
