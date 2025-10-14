using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Guid? ValidateAccessToken(string token);
    }
}