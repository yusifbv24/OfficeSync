using MediatR;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Permissions;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Queries.Users
{
    /// <summary>
    /// Query to retrieve a complete user profile by ID.
    /// </summary>
    public record GetUserProfileByIdQuery(
        Guid UserProfileId):IRequest<Result<UserProfileDto>>;


    public class GetUserProfileByIdQueryHandler:IRequestHandler<GetUserProfileByIdQuery, Result<UserProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserProfileByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UserProfileDto>> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken)
        {
            var userProfile=await _unitOfWork.UserProfiles.GetByIdAsync(
                request.UserProfileId,cancellationToken);

            if (userProfile == null)
            {
                return Result<UserProfileDto>.Failure("User profile not found.");
            }

            // Load role assignment if exists
            var roleAssignment=await _unitOfWork.RoleAssignments.GetFirstOrDefaultAsync(
                ra=>ra.UserProfileId==userProfile.Id,
                cancellationToken);

            // Load permissions if exist
            var permissions=await _unitOfWork.Permissions.GetFirstOrDefaultAsync(
                p=>p.UserProfileId==userProfile.Id,
                cancellationToken);

            // Map to DTO
            PermissionsDto? permissionsDto = null;

            if (permissions != null)
            {
                var channelIds=!string.IsNullOrWhiteSpace(permissions.SpecificChannelIds)
                    ? permissions.SpecificChannelIds.Split(',',StringSplitOptions.RemoveEmptyEntries)
                    : null;

                permissionsDto = new PermissionsDto(
                    CanManageUsers: permissions.CanManageUsers,
                    CanManageChannels: permissions.CanManageChannels,
                    CanDeleteMessages: permissions.CanDeleteMessages,
                    CanManageRoles: permissions.CanManageRoles,
                    SpecificChannelIds: channelIds,
                    ExpiresAt: permissions.ExpiresAt);
            }

            var dto=new UserProfileDto(
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
                Role: roleAssignment?.Role,
                RoleAssignedAt: roleAssignment?.AssignedAt,
                RoleAssignedBy: roleAssignment?.AssignedBy,
                Permissions: permissionsDto);

            return Result<UserProfileDto>.Success(dto);
        }
    }
}