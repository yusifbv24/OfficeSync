using MediatR;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.Queries.Users
{

    /// <summary>
    /// Query to retrieve all users with pagination.
    /// Pagination is essential for performance with large user bases.
    /// </summary>
    public record GetAllUsersQuery(
        int PageNumber,
        int PageSize
    ): IRequest<Result<PagedResult<UserListDto>>>;



    public class GetAllUsersQueryHandler:IRequestHandler<GetAllUsersQuery, Result<PagedResult<UserListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetAllUsersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<UserListDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            // Get all user profiles (in production, this should use a proper pagination query at the database level)
            var allProfiles = await _unitOfWork.UserProfiles.GetAllAsync(cancellationToken);

            // Filter out deleted users
            var activeProfiles = allProfiles
                .Where(p => p.Status != UserStatus.Deleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var totalCount = activeProfiles.Count;


            // Apply pagination
            var pagedProfiles = activeProfiles
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Load roles for these users
            var profileIds = pagedProfiles.Select(p => p.Id).ToList();
            var roleAssignments = await _unitOfWork.RoleAssignments.FindAsync(
                ra => profileIds.Contains(ra.UserProfileId),
                cancellationToken);

            var roleDict = roleAssignments.ToDictionary(ra => ra.UserProfileId, ra => ra.Role);

            // Map To DTO
            var dtos = pagedProfiles.Select(p => new UserListDto(
                Id: p.Id,
                UserId: p.UserId,
                DisplayName: p.DisplayName,
                AvatarUrl: p.AvatarUrl,
                Status: p.Status,
                Role: roleDict.ContainsKey(p.Id) ? roleDict[p.Id] : null,
                CreatedAt: p.CreatedAt,
                LastSeenAt: p.LastSeenAt
            )).ToList();

            var pagedResult = PagedResult<UserListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<UserListDto>>.Success(pagedResult);
        }
    }
}