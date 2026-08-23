namespace Cogniva.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
