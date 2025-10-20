using ChannelService.Domain.Entities;
using ChannelService.Domain.Enums;
using ChannelService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChannelService.Infrastructure.Data
{
    /// <summary>
    /// Database initializer with seed data.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ChannelDbContext context,
            ILogger logger)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied succesfully");

                if(await context.Channels.AnyAsync())
                {
                    logger?.LogInformation("Database already contains channels,skipping seed data");
                    return;
                }
                await SeedChannelsAsync(context, logger);

                logger?.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        private static async Task SeedChannelsAsync(ChannelDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding channels...");

            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // Create general public channel
            var generalChannel = Channel.Create(
                name: ChannelName.Create("General"),
                type: ChannelType.Public,
                createdBy: adminUserId,
                description: "General discussion channel for all users");

            // Create announcements channel
            var announcementsChanels = Channel.Create(
                name: ChannelName.Create("Announcements"),
                type: ChannelType.Public,
                createdBy: adminUserId,
                description: "Official announcements and updates");

            // Create random channel
            var randomChannel = Channel.Create(
                name: ChannelName.Create("Random"),
                type: ChannelType.Public,
                createdBy: adminUserId,
                description: "Off-topic discussions and casual conversations");

            await context.Channels.AddRangeAsync(generalChannel, announcementsChanels, randomChannel);
            await context.SaveChangesAsync();

            logger?.LogInformation("Seeded {Count} channels succesfully", 3);
        }
    }
}