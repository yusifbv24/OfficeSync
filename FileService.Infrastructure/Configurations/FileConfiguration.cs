using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using File = FileService.Domain.Entities.File;

namespace FileService.Infrastructure.Configurations
{
    public class FileConfiguration:IEntityTypeConfiguration<File>
    {
        public void Configure(EntityTypeBuilder<File> builder)
        {
            builder.ToTable("files");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e => e.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();

            builder.Property(e => e.StoredFileName)
                .HasColumnName("stored_file_name")
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.FilePath)
                .HasColumnName("file_path")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.ThumbnailPath)
                .HasColumnName("thumbnail_path")
                .HasMaxLength(500);

            builder.Property(e => e.FileHash)
                .HasColumnName("file_hash")
                .HasMaxLength(64)  // SHA-256 produces 64 hex characters
                .IsRequired();

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            // Configure numeric properties
            builder.Property(e => e.SizeInBytes)
                .HasColumnName("size_in_bytes")
                .IsRequired();

            builder.Property(e => e.DownloadCount)
                .HasColumnName("download_count")
                .HasDefaultValue(0)
                .IsRequired();

            // Configure foreign key properties
            builder.Property(e => e.UploadedBy)
                .HasColumnName("uploaded_by")
                .IsRequired();

            builder.Property(e => e.ChannelId)
                .HasColumnName("channel_id");

            builder.Property(e => e.MessageId)
                .HasColumnName("message_id");

            builder.Property(e => e.DeletedBy)
                .HasColumnName("deleted_by");

            builder.Property(e => e.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.ScannedAt)
                .HasColumnName("scanned_at")
                .HasColumnType("timestamp with time zone");

            // Configure boolean properties
            builder.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(e => e.IsScanned)
                .HasColumnName("is_scanned");

            // Configure enum property - stored as integer in database
            builder.Property(e => e.AccessLevel)
                .HasColumnName("access_level")
                .HasConversion<int>()  // Convert enum to int for storage
                .IsRequired();

            // Configure timestamps from BaseEntity
            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Configure relationship with FileAccess
            // One file can have many access grants
            builder.HasMany(e => e.FileAccesses)
                .WithOne(e => e.File)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);  // Delete access grants when file is deleted

            // Index on UploadedBy for "my files" queries
            builder.HasIndex(e => e.UploadedBy)
                .HasDatabaseName("ix_files_uploaded_by");

            // Index on ChannelId for channel file listings
            builder.HasIndex(e => e.ChannelId)
                .HasDatabaseName("ix_files_channel_id");

            // Index on MessageId for message attachment lookups
            builder.HasIndex(e => e.MessageId)
                .HasDatabaseName("ix_files_message_id");

            // Index on FileHash for deduplication checks
            builder.HasIndex(e => e.FileHash)
                .HasDatabaseName("ix_files_file_hash");

            // Composite index for common filtering scenarios
            // This speeds up queries that filter by both date and uploader
            builder.HasIndex(e => new { e.UploadedAt, e.UploadedBy })
                .HasDatabaseName("ix_files_uploaded_at_uploaded_by");

            // Index on IsDeleted for query filter performance
            // Since we filter by IsDeleted in almost every query, indexing it helps
            builder.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("ix_files_is_deleted");
        }
    }
}