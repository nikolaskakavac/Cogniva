using Cogniva.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cogniva.Api.Data.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Name).HasMaxLength(255).IsRequired();
        builder.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.FileType).HasMaxLength(50).IsRequired();
        builder.Property(document => document.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(document => new { document.UserId, document.UploadedAt });

        builder.HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.Document)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
