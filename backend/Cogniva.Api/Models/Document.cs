namespace Cogniva.Api.Models;

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileType { get; set; }
    public required string FilePath { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string? Summary { get; set; }
    public string? ProcessingError { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
    public ICollection<ConversationDocument> ConversationDocuments { get; set; } = [];
    public ICollection<MessageSource> MessageSources { get; set; } = [];
}
