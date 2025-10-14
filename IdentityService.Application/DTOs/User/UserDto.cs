namespace IdentityService.Application.DTOs.User
{
    public record UserDto(
        Guid Id,
        string Username,
        string Email,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? LastLoginAt);
}