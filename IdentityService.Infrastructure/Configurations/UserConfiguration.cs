using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Infrastructure.Configurations
{
    public class UserConfiguration:IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("id")
                   .IsRequired();

            builder.Property(x => x.Username)
                   .HasColumnName("username")
                   .IsRequired()
                   .HasMaxLength(30);

            builder.Property(x=> x.Email)
                   .HasColumnName("email")
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.PasswordHash)
                   .HasColumnName("password_hash")
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.IsActive)
                   .HasColumnName("is_active")
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                   .HasColumnName("created_at")
                   .IsRequired()
                   .HasColumnType("timestamptz");

            builder.Property(x => x.UpdatedAt)
                   .HasColumnName("updated_at")
                   .IsRequired()
                   .HasColumnType("timestamptz");

            builder.Property(x => x.LastLoginAt)
                   .HasColumnName("last_login_at")
                   .HasColumnType("timestamptz");


            // Indexes
            builder.HasIndex(e => e.Username)
                   .IsUnique()
                   .HasDatabaseName("ix_users_username");

            builder.HasIndex(e => e.Email)
                   .IsUnique()
                   .HasDatabaseName("ix_users_email");

            builder.HasIndex(e => e.IsActive)
                   .HasDatabaseName("ix_users_is_active");

            // Relationships
            builder.HasMany(e => e.RefreshTokens)
                   .WithOne(rt => rt.User)
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // Delete all tokens when user is deleted
        }
    }
}