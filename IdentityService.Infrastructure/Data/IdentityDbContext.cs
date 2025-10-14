using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Data
{
    public class IdentityDbContext:DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options):base(options)
        {
        }
        public DbSet<RefreshToken> RefreshTokens=>Set<RefreshToken>();
        public DbSet<User> Users => Set<User>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from separate configuration classes
            // This keeps our DbContext clean and follows the Single Responsibility Principle
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        }
    }
}