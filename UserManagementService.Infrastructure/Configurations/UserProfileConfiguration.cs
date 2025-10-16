using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Infrastructure.Configurations
{
    public class UserProfileConfiguration:IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("user_profiles");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e=>e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(e=>e.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e=>e.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(e=>e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(UserStatus.Active);

            builder.Property(e => e.LastSeenAt)
                .HasColumnName("last_seen_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("ix_user_profiles_user_id");

            builder.HasIndex(e => e.DisplayName)
                .IsUnique()
                .HasDatabaseName("ix_user_profiles_display_name");

            builder.HasIndex(e => e.Status)
                .HasDatabaseName("ix_user_profiles_status");

            // Relationships
            builder.HasOne(e => e.RoleAssignment)
                .WithOne(ra => ra.UserProfile)
                .HasForeignKey<UserRoleAssignment>(ra => ra.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.HasMany(e => e.Permissions)
                .WithOne(p => p.UserProfile)
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}