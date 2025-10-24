using MessagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MessagingService.Infrastructure.Configurations
{
    public class MessageReadReceiptConfiguration:IEntityTypeConfiguration<MessageReadReceipt>
    {
        public void Configure(EntityTypeBuilder<MessageReadReceipt> builder)
        {
            builder.ToTable("message_read_receipts");


            builder.HasKey(e=>e.Id);


            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();


            builder.Property(e=>e.MessageId)
                .HasColumnName("message_id")
                .IsRequired();


            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();



            builder.Property(e => e.ReadAt)
                .HasColumnName("read_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");



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
                .HasDatabaseName("ix_message_read_receipts_message_id");


            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_message_read_receipts_user_id");


            // Composite index for checking if specific user read specific message
            builder.HasIndex(e => new { e.MessageId, e.UserId })
                .HasDatabaseName("ix_message_read_receipts_message_user")
                .IsUnique(); // Each user can only have one read receipt per message


            builder.HasIndex(e => e.ReadAt)
                .HasDatabaseName("ix_message_read_receipts_read_at");
        }
    }
}