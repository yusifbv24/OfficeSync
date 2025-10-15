using UserManagementService.Domain.Common;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Domain.Entities
{
    /// <summary>
    /// Represents the assignment of a role to a user.
    /// This is a separate entity to track who assigned the role and when,
    /// which is important for audit trails and security.
    /// </summary>
    public class UserRoleAssignment:BaseEntity
    {
        /// <summary>
        /// The user who has been assigned this role.
        /// </summary>
        public Guid UserProfileId { get; set; }



        /// <summary>
        /// The role assigned to the user.
        /// </summary>
        public UserRole Role { get; set; }



        /// <summary>
        /// The admin who assigned this role.
        /// This creates accountability for role assignments.
        /// </summary>
        public Guid AssignedBy { get; set; }



        /// <summary>
        /// When the role was assigned.
        /// Inherited from BaseEntity but semantically important here.
        /// </summary>
        public DateTime AssignedAt { get; set; }



        /// <summary>
        /// Optional reason for the role assignment.
        /// Useful for audit trails and understanding organizational structure.
        /// </summary>
        public string? Reason { get; set; }



        /// <summary>
        /// Navigation property back to the user profile.
        /// </summary>
        public UserProfile UserProfile { get; set; } = null!;
    }
}