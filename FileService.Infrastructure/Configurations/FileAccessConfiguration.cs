using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FileAccess = FileService.Domain.Entities.FileAccess;

namespace FileService.Infrastructure.Configurations
{
    public class FileAccessConfiguration:IEntityTypeConfiguration<FileAccess>
    {
        public void Configure(EntityTypeBuilder<FileAccess> builder)
        {
            builder.ToTable("file_accesses");

            // Configure primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            // Configure foreign keys
            builder.Property(e => e.FileId)
                .HasColumnName("file_id")
                .IsRequired();

            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(e => e.GrantedBy)
                .HasColumnName("granted_by")
                .IsRequired();

            builder.Property(e => e.RevokedBy)
                .HasColumnName("revoked_by");

            // Configure datetime properties
            builder.Property(e => e.GrantedAt)
                .HasColumnName("granted_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e => e.RevokedAt)
                .HasColumnName("revoked_at")
                .HasColumnType("timestamp with time zone");

            // Configure boolean properties
            builder.Property(e => e.IsRevoked)
                .HasColumnName("is_revoked")
                .HasDefaultValue(false)
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

            // Configure indexes for query performance
            // Composite index on FileId and UserId since we often query by both
            // This speeds up queries like "does user X have access to file Y"
            builder.HasIndex(e => new { e.FileId, e.UserId })
                .HasDatabaseName("ix_file_accesses_file_user");

            // Index on UserId for queries like "what files does user X have access to"
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_file_accesses_user_id");

            // Index on IsRevoked for filtering active vs revoked access grants
            builder.HasIndex(e => e.IsRevoked)
                .HasDatabaseName("ix_file_accesses_is_revoked");
        }
    }
}