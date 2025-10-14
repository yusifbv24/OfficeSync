namespace IdentityService.Application.DTOs.User
{
    public record UpdateUserRequestDto(
        string? Username,
        string? Email);
}