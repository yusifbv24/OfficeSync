using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Application.Interfaces;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.Commands.Roles
{
    /// <summary>
    /// Command to assign a role to a user.
    /// Only admins can assign roles in the system.
    /// </summary>
    public record AssignRoleCommand(
        Guid UserProfileId,
        UserRole Role,
        Guid AssignedBy,
        string? Reason
    ) : IRequest<Result<bool>>;


    public class AssignRoleCommandValidator:AbstractValidator<AssignRoleCommand>
    {
        public AssignRoleCommandValidator()
        {
            RuleFor(x=>x.UserProfileId)
                .NotEmpty().WithMessage("UserProfileId is required.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role specified");

            RuleFor(x => x.AssignedBy)
                .NotEmpty().WithMessage("AssignedBy is required");
        }
    }


    public class AssignRoleCommandHandler:IRequestHandler<AssignRoleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignRoleCommandHandler> _logger;

        public AssignRoleCommandHandler(IUnitOfWork unitOfWork,ILogger<AssignRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<bool>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            // Verify the user profile exists
            var userProfile = await _unitOfWork.UserProfiles.GetByIdAsync(
                request.UserProfileId,
                cancellationToken);

            if (userProfile == null)
            {
                return Result<bool>.Failure("User profile not found.");
            }

            // Check if user already has a role assignment
            var existingAssignment=await _unitOfWork.RoleAssignments.GetFirstOrDefaultAsync(
                ra => ra.UserProfileId == request.UserProfileId,
                cancellationToken);

            if (existingAssignment != null)
            {
                // Update existing assignment
                existingAssignment.Role= request.Role;
                existingAssignment.AssignedBy= request.AssignedBy;
                existingAssignment.AssignedAt= DateTime.UtcNow;
                existingAssignment.Reason= request.Reason;
                existingAssignment.UpdatedAt= DateTime.UtcNow;

                await _unitOfWork.RoleAssignments.UpdateAsync(existingAssignment, cancellationToken);
            }
            else
            {
                // Create new role assignment
                var roleAssignment = new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = request.UserProfileId,
                    Role = request.Role,
                    AssignedBy = request.AssignedBy,
                    AssignedAt = DateTime.UtcNow,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.RoleAssignments.AddAsync(roleAssignment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Role {Role} assigned to user {UserProfileId} by {AssignedBy}",
                request.Role,
                request.UserProfileId,
                request.AssignedBy);

            return Result<bool>.Success(true, $"Role {request.Role} assigned successfully");
        }
    }
}