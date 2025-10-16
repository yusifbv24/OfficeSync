using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Commands.Roles
{
    /// <summary>
    /// Command to remove a role from a user.
    /// This reverts the user to having no explicit role.
    /// </summary>
    public record RemoveRoleCommand(
        Guid UserProfileId,
        Guid RemovedBy
    ) : IRequest<Result<bool>>;


    public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveRoleCommandHandler> _logger;
        public RemoveRoleCommandHandler(IUnitOfWork unitOfWork, ILogger<RemoveRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
        {
            var roleAssignment=await _unitOfWork.RoleAssignments.GetFirstOrDefaultAsync(
                ra => ra.UserProfileId == request.UserProfileId,
                cancellationToken);

            if(roleAssignment == null)
            {
                return Result<bool>.Failure("User does not have a role assignment");
            }

            await _unitOfWork.RoleAssignments.DeleteAsync(roleAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                "Role removed from user {UserProfileId} by {RemovedBy}",
                request.UserProfileId,
                request.RemovedBy);

            return Result<bool>.Success(true, "Role removed successfully");
        }
    }
}