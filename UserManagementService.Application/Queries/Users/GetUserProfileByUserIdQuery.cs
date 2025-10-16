using MediatR;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Permissions;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Queries.Users
{
    /// <summary>
    /// Query to retrieve a user profile by the UserId from Identity Service.
    /// This is used for inter-service communication.
    /// </summary>
    public record GetUserProfileByUserIdQuery(
        Guid UserId):IRequest<Result<UserProfileDto>>;


    public class GetUserProfileByUserIdQueryHandler:IRequestHandler<GetUserProfileByUserIdQuery, Result<UserProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserProfileByUserIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork= unitOfWork;
        }

        public async Task<Result<UserProfileDto>> Handle(GetUserProfileByUserIdQuery request, CancellationToken cancellationToken)
        {
            var userProfile=await _unitOfWork.UserProfiles.GetFirstOrDefaultAsync(
                up=>up.UserId==request.UserId,
                cancellationToken);


            if (userProfile == null)
            {
                return Result<UserProfileDto>.Failure("User profile not found.");
            }

            // Load role and permissions
            var roleAssignment=await _unitOfWork.RoleAssignments.GetFirstOrDefaultAsync(
                ra=>ra.UserProfileId==userProfile.Id,
                cancellationToken);

            var permissions = await _unitOfWork.Permissions.GetFirstOrDefaultAsync(
                p => p.UserProfileId == userProfile.Id,
                cancellationToken);


            PermissionsDto? permissionsDto = null;
            if (permissions != null)
            {
                var channelIds=!string.IsNullOrWhiteSpace(permissions.SpecificChannelIds)
                    ? permissions.SpecificChannelIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    : null;

                permissionsDto=new PermissionsDto(
                    CanManageUsers: permissions.CanManageUsers,
                    CanManageChannels: permissions.CanManageChannels,
                    CanDeleteMessages: permissions.CanDeleteMessages,
                    CanManageRoles: permissions.CanManageRoles,
                    SpecificChannelIds: channelIds,
                    ExpiresAt: permissions.ExpiresAt);
            }

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
                Role: roleAssignment?.Role,
                RoleAssignedAt: roleAssignment?.AssignedAt,
                RoleAssignedBy: roleAssignment?.AssignedBy,
                Permissions: permissionsDto);

            return Result<UserProfileDto>.Success(dto);
        }
    }
}