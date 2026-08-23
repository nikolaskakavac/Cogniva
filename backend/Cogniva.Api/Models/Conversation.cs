namespace Cogniva.Api.Models;

public sealed class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<ConversationDocument> ConversationDocuments { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
