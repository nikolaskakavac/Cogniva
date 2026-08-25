namespace Cogniva.Api.DTOs.Conversations;

public sealed record ConversationListItemResponse(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MessageCount,
    IReadOnlyList<string> DocumentNames);
