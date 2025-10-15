namespace UserManagementService.Application.DTOs.Permissions
{
    /// <summary>
    /// Request to grant specific permissions to a user.
    /// Admins can grant any permissions.
    /// Operators can only grant limited permissions within their scope.
    /// </summary>
    public record GrantPermissionsRequestDto(
        bool CanManageUsers,
        bool CanManageChannels,
        bool CanDeleteMessages,
        bool CanManageRoles,
        string[]? SpecificChannelIds,
        DateTime? ExpiresAt
    );
}