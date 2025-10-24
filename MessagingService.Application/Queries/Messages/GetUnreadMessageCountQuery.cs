using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;

namespace MessagingService.Application.Queries.Messages
{
    public record GetUnreadMessageCountQuery(
        Guid ChannelId,
        Guid UserId
    ):IRequest<Result<int>>;




    public class GetUnreadMessageCountQueryHandler: IRequestHandler<GetUnreadMessageCountQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUnreadMessageCountQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork=unitOfWork;
        }

        public async Task<Result<int>> Handle(
            GetUnreadMessageCountQuery request,
            CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Messages
                .GetQueryable()
                .Where(m => m.ChannelId == request.ChannelId)
                .Where(m => m.SenderId != request.UserId)
                .Where(m => !m.IsDeleted)
                .Where(m => !m.ReadReceipts.Any(r => r.UserId == request.UserId));

            var count=await _unitOfWork.Messages.CountAsync(query, cancellationToken);

            return Result<int>.Success(count);
        }
    }
}