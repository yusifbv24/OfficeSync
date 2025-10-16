using FluentValidation;
using MediatR;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Commands.Users
{
    /// <summary>
    /// Command to update a user's profile information.
    /// </summary>
    /// </summary>
    public record UpdateUserCommand(
        Guid UserProfileId,
        string? DisplayName,
        string? AvatarUrl,
        string? Notes
    ) : IRequest<Result<UserProfileDto>>;


    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            When(x => !string.IsNullOrWhiteSpace(x.DisplayName), () =>
            {
                RuleFor(x => x.DisplayName)
                    .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
                    .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");
            });
        }
    }


    public class UpdateUserCommandHandler:IRequestHandler<UpdateUserCommand, Result<UserProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public UpdateUserCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<Result<UserProfileDto>> Handle(UpdateUserCommand request,CancellationToken cancellationToken)
        {
            var userProfile = await _unitOfWork.UserProfiles.GetByIdAsync(
                request.UserProfileId,
                cancellationToken);

            if (userProfile == null)
            {
                return Result<UserProfileDto>.Failure("User profile not found");
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                // Check if display name is already taken by another user
                var existingProfile = await _unitOfWork.UserProfiles.GetFirstOrDefaultAsync(
                    p => p.DisplayName == request.DisplayName && p.Id != request.UserProfileId,
                    cancellationToken);

                if (existingProfile != null)
                {
                    return Result<UserProfileDto>.Failure("Display name is already taken");
                }

                userProfile.DisplayName = request.DisplayName;
            }

            if (request.AvatarUrl != null)
            {
                userProfile.AvatarUrl = request.AvatarUrl;
            }

            if (request.Notes != null)
            {
                userProfile.Notes = request.Notes;
            }

            userProfile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.UserProfiles.UpdateAsync(userProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);


            // Map to DTO with role and permissions if they exist
            var role = userProfile.RoleAssignment?.Role;
            var roleAssignedAt = userProfile.RoleAssignment?.AssignedAt;
            var roleAssignedBy = userProfile.RoleAssignment?.AssignedBy;

            var dto = new UserProfileDto(
                Id: userProfile.Id,
                UserId: userProfile.UserId,
                DisplayName: userProfile.DisplayName,
                AvatarUrl: userProfile.AvatarUrl,
                Status: userProfile.Status,
                CreatedAt: userProfile.CreatedAt,
                UpdatedAt: userProfile.UpdatedAt,
                LastSeenAt: userProfile.LastSeenAt,
                CreatedBy: userProfile.CreatedBy,
                Notes: userProfile.Notes,
                Role: role,
                RoleAssignedAt: roleAssignedAt,
                RoleAssignedBy: roleAssignedBy,
                Permissions: null // Will be loaded separately if needed
            );

            return Result<UserProfileDto>.Success(dto, "User profile updated successfully");
        }
    }
}