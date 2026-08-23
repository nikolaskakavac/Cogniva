using Pgvector;

namespace Cogniva.Api.Models;

public sealed class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public required string Content { get; set; }
    public int ChunkIndex { get; set; }
    public int? PageNumber { get; set; }
    public Vector? Embedding { get; set; }

    public Document Document { get; set; } = null!;
    public ICollection<MessageSource> MessageSources { get; set; } = [];
}
