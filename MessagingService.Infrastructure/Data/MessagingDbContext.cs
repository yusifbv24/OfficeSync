using MessagingService.Domain.Entities;
using MessagingService.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MessagingService.Infrastructure.Data
{
    public class MessagingDbContext:DbContext
    {
        public MessagingDbContext(DbContextOptions<MessagingDbContext> options):base(options)
        {
            
        }
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<MessageReaction> Reactions => Set<MessageReaction>();
        public DbSet<MessageReadReceipt> ReadReceipt => Set<MessageReadReceipt>(); 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new MessageConfiguration());
            modelBuilder.ApplyConfiguration(new MessageReactionConfiguration());
            modelBuilder.ApplyConfiguration(new MessageReadReceiptConfiguration());
        }
    }
}