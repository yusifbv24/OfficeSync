using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementService.Domain.Entities;

namespace UserManagementService.Infrastructure.Configurations
{
    public class UserRoleAssignmentConfiguration:IEntityTypeConfiguration<UserRoleAssignment>
    {
        public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
        {
            builder.ToTable("user_role_assignments");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e=> e.UserProfileId)
                .HasColumnName("user_profile_id")
                .IsRequired();

            builder.Property(e => e.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e=>e.AssignedBy)
                .HasColumnName("assigned_by")
                .IsRequired();

            builder.Property(e=>e.AssignedAt)
                .HasColumnName("assigned_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(500);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(e => e.UserProfileId)
                .IsUnique()
                .HasDatabaseName("ix_role_assignments_user_profile_id");

            builder.HasIndex(e => e.Role)
                .HasDatabaseName("ix_role_assignments_role");
        }
    }
}