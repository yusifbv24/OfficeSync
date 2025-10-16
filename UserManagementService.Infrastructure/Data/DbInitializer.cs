using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Infrastructure.Data;

/// <summary>
/// Database initializer that creates seed data.
/// Ensures the system has essential records on first startup.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initialize the database with seed data.
    /// Safe to call multiple times - only creates data if it doesn't exist.
    /// </summary>
    public static async Task InitializeAsync(
        UserManagementDbContext context,
        ILogger logger)
    {
        try
        {
            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Check if we already have user profiles
            if (await context.UserProfiles.AnyAsync())
            {
                logger.LogInformation("Database already contains user profiles, skipping seed data");
                return;
            }

            // Seed data
            await SeedUserProfilesAsync(context, logger);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    /// <summary>
    /// Seed initial user profiles into the database.
    /// These correspond to the users created in the Identity Service.
    /// </summary>
    private static async Task SeedUserProfilesAsync(
        UserManagementDbContext context,
        ILogger logger)
    {
        logger.LogInformation("Seeding user profiles...");

        // Note: These user IDs should match the users created in Identity Service seed data
        // In a real scenario, you would coordinate this or use a migration script
        // For now, we're using predefined GUIDs that should be updated after Identity Service creates users

        var adminProfileId = Guid.NewGuid();
        var operatorProfileId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        // Create user profiles
        var profiles = new List<UserProfile>
        {
            // Admin user profile
            new UserProfile
            {
                Id = adminProfileId,
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Placeholder - update after Identity Service
                DisplayName = "System Administrator",
                AvatarUrl = null,
                Status = UserStatus.Active,
                CreatedBy = null, // System-created
                Notes = "System administrator account",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Operator user profile
            new UserProfile
            {
                Id = operatorProfileId,
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // Placeholder
                DisplayName = "System Operator",
                AvatarUrl = null,
                Status = UserStatus.Active,
                CreatedBy = adminProfileId,
                Notes = "System operator account for testing",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Regular user profile
            new UserProfile
            {
                Id = userProfileId,
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000003"), // Placeholder
                DisplayName = "Test User",
                AvatarUrl = null,
                Status = UserStatus.Active,
                CreatedBy = adminProfileId,
                Notes = "Test user account",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.UserProfiles.AddRangeAsync(profiles);

        // Create role assignments
        var roleAssignments = new List<UserRoleAssignment>
        {
            // Admin role
            new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserProfileId = adminProfileId,
                Role = UserRole.Admin,
                AssignedBy = adminProfileId, // Self-assigned for system admin
                AssignedAt = DateTime.UtcNow,
                Reason = "System administrator",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Operator role
            new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserProfileId = operatorProfileId,
                Role = UserRole.Operator,
                AssignedBy = adminProfileId,
                AssignedAt = DateTime.UtcNow,
                Reason = "Test operator",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // User role
            new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Role = UserRole.User,
                AssignedBy = adminProfileId,
                AssignedAt = DateTime.UtcNow,
                Reason = "Regular user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.RoleAssignments.AddRangeAsync(roleAssignments);

        // Create some example permissions for the operator
        var permissions = new List<UserPermission>
        {
            new UserPermission
            {
                Id = Guid.NewGuid(),
                UserProfileId = operatorProfileId,
                GrantedBy = adminProfileId,
                CanManageUsers = false,
                CanManageChannels = true,
                CanDeleteMessages = true,
                CanManageRoles = false,
                SpecificChannelIds = null, // Can manage all channels
                ExpiresAt = null, // Never expires
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Permissions.AddRangeAsync(permissions);

        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {ProfileCount} user profiles successfully", profiles.Count);
        logger.LogInformation("Seeded {RoleCount} role assignments successfully", roleAssignments.Count);
        logger.LogInformation("Seeded {PermissionCount} permissions successfully", permissions.Count);
        logger.LogWarning("IMPORTANT: Update UserId placeholders in seed data after Identity Service creates users");
    }
}