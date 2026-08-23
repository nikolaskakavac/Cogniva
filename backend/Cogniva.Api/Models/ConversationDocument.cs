namespace Cogniva.Api.Models;

public sealed class ConversationDocument
{
    public Guid ConversationId { get; set; }
    public Guid DocumentId { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Document Document { get; set; } = null!;
}
