using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.Commands.Users
{
    /// <summary>
    /// Command to delete a user (soft delete).
    /// The user record is marked as deleted but not removed from the database.
    /// This preserves audit trails and historical data.
    /// </summary>
    public record DeleteUserCommand(
        Guid UserProfileId,
        Guid DeletedBy
    ) : IRequest<Result<bool>>;


    public class DeleteUserCommandHandler:IRequestHandler<DeleteUserCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<bool>> Handle(DeleteUserCommand request,CancellationToken cancellationToken)
        {
            var userProfile=await _unitOfWork.UserProfiles.GetByIdAsync(request.UserProfileId, cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User profile not found.");
            }

            // Soft delete : mark as deleted
            userProfile.Status = UserStatus.Deleted;
            userProfile.UpdatedAt= DateTime.UtcNow;

            await _unitOfWork.UserProfiles.UpdateAsync(userProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                    "User {UserId} deleted by {DeletedBy}",
                    userProfile.UserId,
                    request.DeletedBy);

            return Result<bool>.Success(true, "User deleted successfully");
        }
    }
}