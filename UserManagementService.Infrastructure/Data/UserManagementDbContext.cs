using Microsoft.EntityFrameworkCore;
using UserManagementService.Domain.Entities;
using UserManagementService.Infrastructure.Configurations;

namespace UserManagementService.Infrastructure.Data
{
    /// <summary>
    /// Database context for User Management Service.
    /// Represents a session with PostgreSQL database.
    /// </summary>
    public class UserManagementDbContext: DbContext
    {
        public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options): base(options)
        {
            
        }
        public DbSet<UserProfile> UserProfiles=>Set<UserProfile>();
        public DbSet<UserRoleAssignment> RoleAssignments=>Set<UserRoleAssignment>();
        public DbSet<UserPermission> Permissions => Set<UserPermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            modelBuilder.ApplyConfiguration(new UserProfileConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
        }
    }
}