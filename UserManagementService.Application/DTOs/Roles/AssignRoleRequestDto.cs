using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.DTOs.Roles
{
    /// <summary>
    /// Request to assign a role to a user.
    /// Only admins can assign roles.
    /// </summary>
    public record AssignRoleRequestDto(
        UserRole Role,
        string? Reason);
}