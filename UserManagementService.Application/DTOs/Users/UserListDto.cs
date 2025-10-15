using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.DTOs.Users
{
    /// <summary>
    /// Simplified user information for list views.
    /// Contains less information than the full profile to improve performance
    /// when loading many users at once.
    /// </summary>
    public record UserListDto(
        Guid Id,
        Guid UserId,
        string DisplayName,
        string? AvatarUrl,
        UserStatus Status,
        UserRole? Role,
        DateTime CreatedAt,
        DateTime? LastSeenAt);
}