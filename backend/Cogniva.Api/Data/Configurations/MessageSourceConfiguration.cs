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

        builder.HasOne(source => source.Document)
            .WithMany(document => document.MessageSources)
            .HasForeignKey(source => source.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(source => source.DocumentChunk)
            .WithMany(chunk => chunk.MessageSources)
            .HasForeignKey(source => source.DocumentChunkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
