namespace Cogniva.Api.Models;

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation Conversation { get; set; } = null!;
    public ICollection<MessageSource> Sources { get; set; } = [];
}
