using UserManagementService.Application.DTOs.Permissions;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.DTOs.Users
{
    /// <summary>
    /// Complete user profile information including role and permissions.
    /// This is what we return when querying user information.
    /// Never exposes the entity directly to maintain proper separation.
    /// </summary>
    public record UserProfileDto(
        Guid Id,
        Guid UserId,
        string DisplayName,
        string? AvatarUrl,
        UserStatus Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? LastSeenAt,
        Guid? CreatedBy,
        string? Notes,
        UserRole? Role,
        DateTime? RoleAssignedAt,
        Guid? RoleAssignedBy,
        PermissionsDto? Permissions);
}