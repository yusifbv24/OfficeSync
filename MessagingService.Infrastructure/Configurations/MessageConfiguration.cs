using MessagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MessagingService.Infrastructure.Configurations
{
    public class MessageConfiguration:IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("messages");

            builder.HasKey(e => e.Id);


            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();


            builder.Property(e => e.ChannelId)
                .HasColumnName("channel_id")
                .IsRequired();


            builder.Property(e => e.SenderId)
                .HasColumnName("sender_id")
                .IsRequired();


            builder.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired()
                .HasMaxLength(4000);


            builder.Property(e => e.Type)
                .HasColumnName("type")
                .IsRequired()
                .HasConversion<string>();


            builder.Property(e => e.IsEdited)
                .HasColumnName("is_edited")
                .IsRequired()
                .HasDefaultValue(false);


            builder.Property(e => e.EditedAt)
                .HasColumnName("edited_at")
                .HasColumnType("timestamp with time zone");


            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);


            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("timestamp with time zone");


            builder.Property(e => e.ParrentMessageId)
                .HasColumnName("parrent_message_id");


            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");


            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");


            // Indexes for performance
            // Messages are frequently queried by channel
            builder.HasIndex(e => e.ChannelId)
                .HasDatabaseName("ix_messages_channel_id");

            builder.HasIndex(e => e.SenderId)
                .HasDatabaseName("ix_messages_sender_id");

            builder.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("ix_messages_is_deleted");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_messages_created_at");


            // Composite index for the most common query: get messages from channel ordered by time
            builder.HasIndex(e => new { e.ChannelId, e.CreatedAt, e.IsDeleted })
                .HasDatabaseName("ix_messages_channel_created_deleted")
                .IncludeProperties(p => p.Id);


            // Index for threading (finding replies)
            builder.HasIndex(e => e.ParrentMessageId)
                .HasDatabaseName("ix_messages_parent_message_id");


            // Configure navigation properties with explicit backing field mapping
            builder.Metadata
                .FindNavigation(nameof(Message.Reactions))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);


            builder.Metadata
                .FindNavigation(nameof(Message.AttachmentFields))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);


            builder.Metadata
                .FindNavigation(nameof(Message.ReadReceipts))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);


            // Self-referencing relationship for threading
            builder.HasOne(e => e.ParrentMessage)
                .WithMany()
                .HasForeignKey(e => e.ParrentMessageId)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete replies


            builder.HasMany(e => e.Reactions)
                .WithOne(r => r.Message)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(e => e.ReadReceipts)
                .WithOne(r => r.Message)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            

            builder.Ignore(e => e.DomainEvents);
        }
    }
}