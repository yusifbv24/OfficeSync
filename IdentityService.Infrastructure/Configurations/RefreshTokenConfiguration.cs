using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Infrastructure.Configurations
{
    public class RefreshTokenConfiguration:IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                   .HasColumnName("id")
                   .IsRequired();

            builder.Property(e => e.UserId)
                   .HasColumnName("user_id")
                   .IsRequired();

            builder.Property(e => e.Token)
                   .HasColumnName("token")
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(e => e.CreatedAt)
                   .HasColumnName("created_at")
                   .IsRequired()
                   .HasColumnType("timestamptz");

            builder.Property(e => e.RevokedAt)
                   .HasColumnName("revoked_at")
                   .HasColumnType("timestamptz");

            builder.Property(e => e.RevokedByIp)
                   .HasColumnName("revoked_by_ip")
                   .HasMaxLength(30);

            builder.Property(e => e.ReplacedByToken)
                   .HasColumnName("replaced_by_token")
                   .HasMaxLength(500);

            builder.Property(e => e.CreatedByIp)
                   .HasColumnName("created_by_ip")
                   .IsRequired()
                   .HasMaxLength(50);


            // Indexes
            builder.HasIndex(e => e.Token)
                   .HasDatabaseName("ix_refresh_tokens_token");

            builder.HasIndex(e => e.UserId)
                   .HasDatabaseName("ix_refresh_tokens_user_id");

            builder.HasIndex(e => e.ExpiresAt)
                   .HasDatabaseName("ix_refresh_tokens_expires_at");


            // Relationship
            builder.HasOne(e => e.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Ignore computed properties (they're not stored in database)
            builder.Ignore(e => e.IsExpired);
            builder.Ignore(e => e.IsRevoked);
            builder.Ignore(e => e.IsActive);
        }
    }
}