using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IdentityDbContext context,
            IPasswordHasher passwordHasher,
            ILogger logger)
        {
            try
            {
                // Ensure database is created and all migrations are applied
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");

                // Check if we already have users (if yes, skip seeding)
                if(await context.Users.AnyAsync())
                {
                    logger.LogInformation("Database already contains users, skipping seed data");
                    return;
                }

                // Create seed data
                await SeedUserAsync(context,passwordHasher,logger);

                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        private static async Task SeedUserAsync(
            IdentityDbContext context,
            IPasswordHasher passwordHasher,
            ILogger logger)
        {
            logger.LogInformation("Seeding users...");

            var users = new List<User>
        {
            // Default admin user
            new() {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@company.local",
                PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            
            // Test operator user (for development)
            new()
            {
                Id = Guid.NewGuid(),
                Username = "operator",
                Email = "operator@company.local",
                PasswordHash = passwordHasher.HashPassword("Operator@123456"),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            
            // Test regular user (for development)
            new()
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "user@company.local",
                PasswordHash = passwordHasher.HashPassword("User@123456"),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} users successfully", users.Count);
            logger.LogInformation("Default admin credentials - Username: admin, Password: Admin@123456");
        }
    }
}