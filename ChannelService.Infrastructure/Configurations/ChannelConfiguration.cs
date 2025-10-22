using ChannelService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChannelService.Infrastructure.Configurations
{
    public class ChannelConfiguration:IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("channels");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Type)
                .HasColumnName("type")
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.IsArchived)
                .HasColumnName("is_archived")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.CreatedBy)
                .HasColumnName("created_by")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            // Indexes
            builder.HasIndex(e => e.Name)
                .HasDatabaseName("ix_chanels_name");

            builder.HasIndex(e => e.Type)
                .HasDatabaseName("ix_channels_type");

            builder.HasIndex(e => e.IsArchived)
                .HasDatabaseName("ix_channels_is_archived");

            builder.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("ix_channels_created_by");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_channels_created_at");

            // Configure the Members navigation property with explicit backing field mapping
            builder.Metadata
                .FindNavigation(nameof(Channel.Members))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // Relationships
            builder.HasMany(e => e.Members)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events (not persisted)
            builder.Ignore(e => e.DomainEvents);
        }
    }
}