using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementService.Domain.Entities;

namespace UserManagementService.Infrastructure.Configurations
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission> 
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.ToTable("user_permissions");

            builder.HasKey(e=>e.Id);

            builder.Property(e=>e.Id)
                .IsRequired()
                .HasColumnName("id");

            builder.Property(e => e.UserProfileId)
                .HasColumnName("user_profile_id")
                .IsRequired();


            builder.Property(e=>e.GrantedBy)
                .HasColumnName("granted_by")
                .IsRequired();


            builder.Property(e=>e.CanManageUsers)
                .HasColumnName("can_manage_users")
                .IsRequired()
                .HasDefaultValue(false);


            builder.Property(e=>e.CanManageChannels)
                .HasColumnName("can_manage_channels")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e=>e.CanDeleteMessages)
                .HasColumnName("can_delete_messages")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e=>e.CanManageRoles)
                .HasColumnName("can_manage_roles")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.SpecificChannelIds)
                .HasColumnName("specific_channel_ids")
                .HasMaxLength(1000);


            builder.Property(e=>e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(e=>e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(e=>e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Indexes
            builder.HasIndex(e=>e.UserProfileId)
                .HasDatabaseName("idx_user_permissions_user_profile_id");

            builder.HasIndex(e=>e.ExpiresAt)
                .HasDatabaseName("idx_user_permissions_expires_at");

            // Ignore computed properties
            builder.Ignore(e => e.IsActive);
        }
    }
}