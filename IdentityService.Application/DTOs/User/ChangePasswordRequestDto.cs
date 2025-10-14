namespace IdentityService.Application.DTOs.User
{
    public record ChangePasswordRequestDto(
         string CurrentPassword,
         string NewPassword);
}