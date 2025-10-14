namespace IdentityService.Application.DTOs.User
{
    public record CreateUserRequestDto(
        string Username,
        string Email,
        string Password);
}