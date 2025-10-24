using MessagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MessagingService.Infrastructure.Configurations
{
    public class MessageReactionConfiguration:IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> builder)
        {
            builder.ToTable("message_reactions");


            builder.HasKey(x => x.Id);


            builder.Property(e => e.Id)
                .IsRequired()
                .HasColumnName("id");


            builder.Property(e => e.MessageId)
                .IsRequired()
                .HasColumnName("message_id");


            builder.Property(e=>e.UserId)
                .IsRequired()
                .HasColumnName("user_id");


            builder.Property(e => e.Emoji)
                .HasColumnName("emoji")
                .IsRequired()
                .HasMaxLength(10);


            builder.Property(e => e.IsRemoved)
                .HasColumnName("is_removed")
                .IsRequired()
                .HasDefaultValue(false);


            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");


            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");


            // Indexes
            builder.HasIndex(e => e.MessageId)
                .HasDatabaseName("ix_message_reactions_message_id");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_message_reactions_user_id");

            builder.HasIndex(e => e.IsRemoved)
                .HasDatabaseName("ix_message_reactions_is_removed");


            // Composite index for common query: get all reactions for a message grouped by emoji
            builder.HasIndex(e => new { e.MessageId, e.Emoji, e.IsRemoved })
                .HasDatabaseName("ix_message_reactions_message_emoji_removed");


            // Unique constraint: user can only have one active reaction per emoji per message
            // (IsRemoved = false ensures this only applies to active reactions)
            builder.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji, e.IsRemoved })
                .HasDatabaseName("ix_message_reactions_unique_active")
                .HasFilter("is_removed = false")
                .IsUnique();
        }
    }
}