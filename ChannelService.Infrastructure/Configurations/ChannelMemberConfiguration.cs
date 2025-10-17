using ChannelService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChannelService.Infrastructure.Configurations
{
    public class ChannelMemberConfiguration:IEntityTypeConfiguration<ChannelMember>
    {
        public void Configure(EntityTypeBuilder<ChannelMember> builder)
        {
            builder.ToTable("channel_members");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e => e.ChannelId)
                .HasColumnName("channel_id")
                .IsRequired();

            builder.Property(e=>e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(e => e.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.AddedBy)
                .HasColumnName("added_by")
                .IsRequired();

            builder.Property(e => e.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.IsRemoved)
                .HasColumnName("is_removed")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.RemovedAt)
                .HasColumnName("removed_at")
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
            builder.HasIndex(e => e.ChannelId)
                .HasDatabaseName("ix_channel_member_channel_id");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_channel_members_user_id");

            builder.HasIndex(e => e.IsRemoved)
                .HasDatabaseName("ix_channel_members_is_removed");

            // Composite index for common query pattern
            builder.HasIndex(e => new { e.ChannelId, e.UserId, e.IsRemoved })
                .HasDatabaseName("ix_channel_members_channel_user_removed");
        }
    }
}