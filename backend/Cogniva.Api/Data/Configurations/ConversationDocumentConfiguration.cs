using Cogniva.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cogniva.Api.Data.Configurations;

public sealed class ConversationDocumentConfiguration : IEntityTypeConfiguration<ConversationDocument>
{
    public void Configure(EntityTypeBuilder<ConversationDocument> builder)
    {
        builder.HasKey(link => new { link.ConversationId, link.DocumentId });

        builder.HasOne(link => link.Conversation)
            .WithMany(conversation => conversation.ConversationDocuments)
            .HasForeignKey(link => link.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Document)
            .WithMany(document => document.ConversationDocuments)
            .HasForeignKey(link => link.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
