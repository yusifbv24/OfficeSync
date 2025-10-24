using MessagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MessagingService.Infrastructure.Configurations
{
    public class MessageAttachmentConfiguration:IEntityTypeConfiguration<MessageAttachment>
    {
        public void Configure(EntityTypeBuilder<MessageAttachment> builder)
        {
            builder.ToTable("message_attachments");

            builder.HasKey(e=>e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();


            builder.Property(e => e.MessageId)
                .HasColumnName("message_id")
                .IsRequired();


            builder.Property(e => e.FileId)
                .HasColumnName("file_id")
                .IsRequired();


            builder.Property(e => e.FileName)
                .HasColumnName("file_name")
                .IsRequired()
                .HasMaxLength(255);


            builder.Property(e => e.FileUrl)
                .HasColumnName("file_url")
                .IsRequired()
                .HasMaxLength(500);


            builder.Property(e => e.FileSize)
                .HasColumnName("file_size")
                .IsRequired();


            builder.Property(e => e.MimeType)
                .HasColumnName("mime_type")
                .IsRequired()
                .HasMaxLength(100);


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
                .HasDatabaseName("ix_message_attachments_message_id");

            builder.HasIndex(e => e.FileId)
                .HasDatabaseName("ix_message_attachments_file_id");

            builder.HasIndex(e => e.MimeType)
                .HasDatabaseName("ix_message_attachments_mime_type");
        }
    }
}