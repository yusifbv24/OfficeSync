namespace IdentityService.Application.DTOs.Auth
{
    public record LoginResponseDto(
        string AccessToken, 
        string RefreshToken, 
        Guid UserId, 
        string UserName, 
        string Email, 
        DateTime ExpiresAt);
}