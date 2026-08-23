namespace Cogniva.Api.Models;

public sealed class MessageSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentChunkId { get; set; }
    public double RelevanceScore { get; set; }

    public Message Message { get; set; } = null!;
    public Document Document { get; set; } = null!;
    public DocumentChunk DocumentChunk { get; set; } = null!;
}
