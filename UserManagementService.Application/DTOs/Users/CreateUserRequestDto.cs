namespace UserManagementService.Application.DTOs.Users
{
    /// <summary>
    /// Request to create a new user in the system.
    /// This will create both authentication credentials (via Identity Service)
    /// and a user profile (in this service).
    /// </summary>
    public record CreateUserRequestDto(
        string Username,
        string Email,
        string Password,
        string DisplayName,
        string? AvatarUrl,
        string? Notes);
}