using UserManagementService.Domain.Common;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Domain.Entities
{
    /// <summary>
    /// Represents a user's profile information beyond authentication credentials.
    /// This entity is separate from the Identity Service's User entity by design.
    /// The separation allows each service to evolve independently.
    /// </summary>
    public class UserProfile : BaseEntity
    {
        /// <summary>
        /// Reference to the user ID from Identity Service.
        /// This links the profile to authentication credentials.
        /// </summary>
        public Guid UserId { get; set; }


        /// <summary>
        /// Display name shown to other users in the chat.
        /// This can be different from the username used for login.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;


        /// <summary>
        /// User's avatar image URL.
        /// In a real system, this would point to file storage.
        /// </summary>
        public string? AvatarUrl { get; set; }


        /// <summary>
        /// Current status of the user account.
        /// Admins can change this to disable or delete accounts.
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.Active;


        /// <summary>
        /// When the user last accessed the system.
        /// Useful for finding inactive accounts.
        /// </summary>
        public DateTime? LastSeenAt { get; set; }


        /// <summary>
        /// The admin or operator who created this user.
        /// Null for users created during initial system setup.
        /// </summary>
        public Guid? CreatedBy { get; set; }


        /// <summary>
        /// Additional notes about the user (for admin reference).
        /// Could contain information like department, location, etc.
        /// </summary>
        public string? Notes { get; set; }


        /// <summary>
        /// Navigation property to the user's role assignment.
        /// One user has one primary role.
        /// </summary>
        public UserRoleAssignment? RoleAssignment { get; set; }


        /// <summary>
        /// Navigation property to specific permissions granted to this user.
        /// These are in addition to permissions from their role.
        /// </summary>
        public ICollection<UserPermission> Permissions { get; set; } = [];
    }
}