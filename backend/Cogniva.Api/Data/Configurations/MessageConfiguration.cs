using Cogniva.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cogniva.Api.Data.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(message => message.Content).IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });

        builder.HasMany(message => message.Sources)
            .WithOne(source => source.Message)
            .HasForeignKey(source => source.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
