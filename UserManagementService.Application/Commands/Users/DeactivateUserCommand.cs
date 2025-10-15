using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.Commands.Users
{
    /// <summary>
    /// Command to deactivate a user account.
    /// Deactivated users cannot log in but their data is preserved.
    /// This also deactivates the user in the Identity Service.
    /// </summary>
    public record DeactivateUserCommand(
        Guid UserProfileId,
        Guid DeactivatedBy):IRequest<Result<bool>>;


    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityServiceClient _identityServiceClient;
        private readonly ILogger<DeactivateUserCommandHandler> _logger;

        public DeactivateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IIdentityServiceClient identityServiceClient,
            ILogger<DeactivateUserCommandHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _identityServiceClient = identityServiceClient;
            _logger = logger;
        }


        public async Task<Result<bool>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var userProfile=await _unitOfWork.UserProfiles.GetByIdAsync(
                request.UserProfileId,
                cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User profile not found");
            }

            // Update status in this service
            userProfile.Status = UserStatus.Disabled;
            userProfile.UpdatedAt=DateTime.UtcNow;

            await _unitOfWork.UserProfiles.UpdateAsync(userProfile, cancellationToken);

            // Deactivate in Identity Service as well
            var identityResult=await _identityServiceClient.DeactivateUserAsync(
                userProfile.UserId,
                cancellationToken);

            if(!identityResult.IsSuccess)
            {
                _logger.LogError(
                    "Failed to deactivate user in Identity Service: {UserId}",
                    userProfile.UserId);
                return Result<bool>.Failure(
                    "Failed to deactivate user in Identity Service",
                    identityResult.Errors);
            }

            _logger.LogInformation(
            "User {UserId} deactivated by {DeactivatedBy}",
            userProfile.UserId,
            request.DeactivatedBy);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true, "User deactivated successfully");
        }
    }
}