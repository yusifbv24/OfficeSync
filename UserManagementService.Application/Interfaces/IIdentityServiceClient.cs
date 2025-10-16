using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Identity;

namespace UserManagementService.Application.Interfaces
{
    /// <summary>
    /// Interface for communicating with the Identity Service.
    /// This represents the HTTP client that calls Identity Service endpoints.
    /// </summary>
    public interface IIdentityServiceClient
    {
        Task<Result<IdentityUserDto>> CreateUserAsync(
            string username,
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<Result<bool>> DeactivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<Result<bool>> UserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}