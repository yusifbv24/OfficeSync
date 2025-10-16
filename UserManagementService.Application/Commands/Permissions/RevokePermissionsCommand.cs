using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Commands.Permissions
{
    /// <summary>
    /// Command to revoke all specific permissions from a user.
    /// The user will only have permissions from their role after this.
    /// </summary>
    public record RevokePermissionsCommand(
        Guid UserProfileId,
        Guid RevokedBy
    ) : IRequest<Result<bool>>;


    public class RevokePermissionsCommandHandler : IRequestHandler<RevokePermissionsCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RevokePermissionsCommandHandler> _logger;
        public RevokePermissionsCommandHandler(IUnitOfWork unitOfWork, ILogger<RevokePermissionsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result<bool>> Handle(RevokePermissionsCommand request, CancellationToken cancellationToken)
        {
            var permissions = await _unitOfWork.Permissions.GetFirstOrDefaultAsync(
                p => p.UserProfileId == request.UserProfileId,
                cancellationToken);

            if (permissions == null)
            {
                return Result<bool>.Failure("No specific permissions found for the user.");
            }

            await _unitOfWork.Permissions.DeleteAsync(permissions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                    "Permissions revoked from user {UserProfileId} by {RevokedBy}",
                    request.UserProfileId,
                    request.RevokedBy);

            return Result<bool>.Success(true, "Permissions revoked successfully");
        }
    }
}