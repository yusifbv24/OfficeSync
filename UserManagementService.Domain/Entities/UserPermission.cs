using UserManagementService.Domain.Common;

namespace UserManagementService.Domain.Entities
{
    /// <summary>
    /// Represents specific permissions granted to a user beyond their role.
    /// This allows fine-grained access control where needed.
    /// For example, an Operator might be given permission to manage specific channels.
    /// </summary>
    public class UserPermission:BaseEntity
    {
        /// <summary>
        /// The user who has been granted this permission.
        /// </summary>
        public Guid UserProfileId { get; set; }



        /// <summary>
        /// The admin or operator who granted this permission.
        /// Only admins can grant most permissions, but operators can grant limited permissions
        /// within their scope of authority.
        /// </summary>
        public Guid GrantedBy { get; set; }



        /// <summary>
        /// Permission to manage other users (create, edit, deactivate).
        /// This is typically only granted to admins and some operators.
        /// </summary>
        public bool CanManageUsers { get; set; }



        /// <summary>
        /// Permission to manage channels (create, edit, delete, add/remove members).
        /// Operators often have this permission for specific channels.
        /// </summary>
        public bool CanManageChannels { get; set; }



        /// <summary>
        /// Permission to delete messages from channels.
        /// This is a moderation capability typically given to operators.
        /// </summary>
        public bool CanDeleteMessages { get; set; }



        /// <summary>
        /// Permission to assign roles to other users.
        /// This is typically restricted to admins only.
        /// </summary>
        public bool CanManageRoles { get; set; }



        /// <summary>
        /// Specific channel IDs that this permission applies to.
        /// Null or empty means the permission applies to all channels.
        /// This allows operators to manage only certain channels.
        /// Stored as a comma-separated string for simplicity.
        /// In a more complex system, this would be a separate many-to-many relationship.
        /// </summary>
        public string? SpecificChannelIds { get; set; }



        /// <summary>
        /// When this permission expires.
        /// Null means the permission never expires.
        /// Useful for temporary elevated access.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }



        /// <summary>
        /// Check if the permission is still valid (not expired).
        /// </summary>
        public bool IsActive => ExpiresAt == null || DateTime.UtcNow < ExpiresAt;



        /// <summary>
        /// Navigation property back to the user profile.
        /// </summary>
        public UserProfile UserProfile { get; set; } = null!;
    }
}