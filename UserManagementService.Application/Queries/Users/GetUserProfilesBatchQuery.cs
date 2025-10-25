using MediatR;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;

namespace UserManagementService.Application.Queries.Users
{
    public record GetUserProfilesBatchQuery(
        Guid[] UserIds
    ):IRequest<Result<Dictionary<Guid,string>>>;


    public class GetUserProfilesBatchQueryHandler : IRequestHandler<GetUserProfilesBatchQuery, Result<Dictionary<Guid, string>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserProfilesBatchQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Dictionary<Guid, string>>> Handle(
            GetUserProfilesBatchQuery request,
            CancellationToken cancellationToken)
        {
            if (request.UserIds == null || request.UserIds.Length == 0)
            {
                return Result<Dictionary<Guid, string>>.Success(
                    new Dictionary<Guid, string>());
            }

            var uniqueUserIds = request.UserIds.Distinct().ToList();

            var userProfiles = await _unitOfWork.UserProfiles.FindAsync(
                up => uniqueUserIds.Contains(up.UserId),
                cancellationToken);

            var profilesList = userProfiles.ToList();
            if (!profilesList.Any())
            {
                return Result<Dictionary<Guid, string>>.Success(
                    new Dictionary<Guid, string>());
            }

            var result = profilesList.ToDictionary(
                p => p.UserId,
                p => p.DisplayName);

            return Result<Dictionary<Guid, string>>.Success(result);
        }
    }
}
