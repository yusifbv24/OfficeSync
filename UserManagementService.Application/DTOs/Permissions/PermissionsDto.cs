namespace UserManagementService.Application.DTOs.Permissions
{
    /// <summary>
    /// Represents the permissions a user has.
    /// This combines role-based permissions with any additional granted permissions.
    /// </summary>
    public record PermissionsDto(
        bool CanManageUsers,
        bool CanManageChannels,
        bool CanDeleteMessages,
        bool CanManageRoles,
        string[]? SpecificChannelIds,
        DateTime? ExpiresAt
    );
}