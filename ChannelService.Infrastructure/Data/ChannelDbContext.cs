using ChannelService.Domain.Entities;
using ChannelService.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Infrastructure.Data
{
    public class ChannelDbContext:DbContext
    {
        public ChannelDbContext(DbContextOptions<ChannelDbContext> options):base(options)
        {
        }
        public DbSet<Channel> Channels=>Set<Channel>();
        public DbSet<ChannelMember> ChannelMembers=>Set<ChannelMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ChannelConfiguration());
            modelBuilder.ApplyConfiguration(new ChannelMemberConfiguration());
        }
    }
}