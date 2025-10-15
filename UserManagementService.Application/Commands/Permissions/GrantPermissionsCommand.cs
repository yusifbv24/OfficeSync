using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Domain.Entities;

namespace UserManagementService.Application.Commands.Permissions;

/// <summary>
/// Command to grant specific permissions to a user.
/// These permissions are in addition to those provided by their role.
/// </summary>
public record GrantPermissionsCommand(
    Guid UserProfileId,
    Guid GrantedBy,
    bool CanManageUsers,
    bool CanManageChannels,
    bool CanDeleteMessages,
    bool CanManageRoles,
    string[]? SpecificChannelIds,
    DateTime? ExpiresAt
) : IRequest<Result<bool>>;

public class GrantPermissionsCommandHandler:IRequestHandler<GrantPermissionsCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GrantPermissionsCommandHandler> _logger;

    public GrantPermissionsCommandHandler(IUnitOfWork unitOfWork,ILogger<GrantPermissionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<Result<bool>> Handle(GrantPermissionsCommand request, CancellationToken cancellationToken)
    {
        // Verify the user profile exists
        var userProfile=await _unitOfWork.UserProfiles.GetByIdAsync(request.UserProfileId,cancellationToken);

        if (userProfile == null)
        {
            return Result<bool>.Failure("User profile not found.");
        }

        // Check if permissions already exists
        var existingPermissions=await _unitOfWork.Permissions.GetFirstOrDefaultAsync(
            p => p.UserProfileId == request.UserProfileId,
            cancellationToken);

        // Convert channel IDs array to comma-separated string
        var channelIdsString=request.SpecificChannelIds!=null&&request.SpecificChannelIds.Length>0
            ? string.Join(",",request.SpecificChannelIds)
            : null; 

        if(existingPermissions != null)
        {
            // Update existing permissions
            existingPermissions.CanManageUsers=request.CanManageUsers;
            existingPermissions.CanManageChannels=request.CanManageChannels;
            existingPermissions.CanDeleteMessages=request.CanDeleteMessages;
            existingPermissions.CanManageRoles=request.CanManageRoles;
            existingPermissions.SpecificChannelIds=channelIdsString;
            existingPermissions.ExpiresAt=request.ExpiresAt;
            existingPermissions.GrantedBy=request.GrantedBy;
            existingPermissions.UpdatedAt=DateTime.UtcNow;

            await _unitOfWork.Permissions.UpdateAsync(existingPermissions,cancellationToken);
        }

        else
        {
            // Create new permissions
            var newPermissions=new UserPermission
            {
                Id=Guid.NewGuid(),
                UserProfileId=request.UserProfileId,
                CanManageUsers=request.CanManageUsers,
                CanManageChannels=request.CanManageChannels,
                CanDeleteMessages=request.CanDeleteMessages,
                CanManageRoles=request.CanManageRoles,
                SpecificChannelIds=channelIdsString,
                ExpiresAt=request.ExpiresAt,
                GrantedBy=request.GrantedBy,
                CreatedAt=DateTime.UtcNow,
                UpdatedAt=DateTime.UtcNow
            };
            await _unitOfWork.Permissions.AddAsync(newPermissions,cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Permissions granted to user {UserProfileId} by {GrantedBy}",
            request.UserProfileId,
            request.GrantedBy);

        return Result<bool>.Success(true, "Permissions granted successfully");
    }
}