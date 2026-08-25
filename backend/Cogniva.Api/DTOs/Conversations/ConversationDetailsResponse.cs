namespace Cogniva.Api.DTOs.Conversations;

public sealed record ConversationDetailsResponse(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ConversationDocumentResponse> Documents,
    IReadOnlyList<MessageResponse> Messages);
