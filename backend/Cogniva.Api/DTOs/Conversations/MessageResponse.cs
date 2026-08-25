namespace Cogniva.Api.DTOs.Conversations;

public sealed record MessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<MessageSourceResponse> Sources);
