namespace UserManagementService.Application.DTOs.Identity
{
    /// <summary>
    /// DTO for user data returned from Identity Service.
    /// </summary>
    public record IdentityUserDto(
        Guid UserId,
        string Username,
        string Email
    );
}