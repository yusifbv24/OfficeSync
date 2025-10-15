namespace UserManagementService.Application.DTOs.Users
{
    /// <summary>
    /// Request to update an existing user's profile information.
    /// All fields are optional - only provided fields will be updated.
    /// </summary>
    public record UpdateUserRequestDto(
        string? DisplayName,
        string? AvatarUrl,
        string? Notes);
}