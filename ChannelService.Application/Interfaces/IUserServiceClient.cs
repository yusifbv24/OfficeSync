using ChannelService.Application.Common;

namespace ChannelService.Application.Interfaces
{
    /// <summary>
    /// Interface for communicating with User Management Service.
    /// </summary>
    public interface IUserServiceClient
    {
        Task<Result<bool>> UserExistsAsync(Guid userId, CancellationToken cancellationToken);

        Task<Result<string>> GetUserDisplayNameAsync(Guid userId,CancellationToken cancellationToken);
    }
}