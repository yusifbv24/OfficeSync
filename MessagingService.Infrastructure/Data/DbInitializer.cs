using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MessagingService.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            MessagingDbContext context,
            ILogger logger)
        {
            try
            {
                // Apply any pending migrations
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migrations applied succesfully");

                // Check if we already have messages (database is not empty)
                if(await context.Messages.AnyAsync())
                {
                    logger?.LogInformation("Database already contains messages, skipping seed data");
                    return;
                }

                // Messages are created by users during normal operation
                // But we keep this infrastructure in place for future needs

                logger?.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occured while initializing the database");
                throw;
            }
        }
    }
}