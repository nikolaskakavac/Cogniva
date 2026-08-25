using Cogniva.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cogniva.Api.Data.Configurations;

public sealed class MessageSourceConfiguration : IEntityTypeConfiguration<MessageSource>
{
    public void Configure(EntityTypeBuilder<MessageSource> builder)
    {
        builder.HasKey(source => source.Id);
        builder.HasIndex(source => source.MessageId);
        builder.HasIndex(source => new { source.MessageId, source.DocumentChunkId }).IsUnique();

        builder.HasOne(source => source.Document)
            .WithMany(document => document.MessageSources)
            .HasForeignKey(source => source.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(source => source.DocumentChunk)
            .WithMany(chunk => chunk.MessageSources)
            .HasForeignKey(source => source.DocumentChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
