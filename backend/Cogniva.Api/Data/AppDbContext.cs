using Cogniva.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cogniva.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationDocument> ConversationDocuments => Set<ConversationDocument>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageSource> MessageSources => Set<MessageSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
