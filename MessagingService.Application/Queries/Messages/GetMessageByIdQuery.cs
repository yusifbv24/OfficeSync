using AutoMapper;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;

namespace MessagingService.Application.Queries.Messages
{
    /// <summary>
    /// Query to get a single message by its ID.
    /// Returns complete message details including reactions and attachments.
    /// </summary>
    public record GetMessageByIdQuery(
        Guid MessageId,
        Guid RequestedBy):IRequest<Result<MessageDto>>;




    public class GetMessageByIdQueryHandler : IRequestHandler<GetMessageByIdQuery, Result<MessageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;

        public GetMessageByIdQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient=channelServiceClient;
            _userServiceClient=userServiceClient;
            _mapper=mapper;
        }

        public async Task<Result<MessageDto>> Handle(
            GetMessageByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Build query using IQueryable - no database hit yet
            var query = _unitOfWork.Messages
                .GetQueryable()
                .Where(m => m.Id == request.MessageId);

            // Execute query - now we hit the database
            var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                request.MessageId,
                cancellationToken,
                m => m.Reactions,
                m => m.AttachmentFields,
                m => m.ReadReceipts);

            if (message == null)
            {
                return Result<MessageDto>.Failure("Message not found");
            }

            // Verify the requesting user is a member of channel
            var isMember = await _channelServiceClient.IsUserMemberOfChannelAsync(
                message.ChannelId,
                request.RequestedBy,
                cancellationToken);

            if(!isMember.IsSuccess || !isMember.Data)
            {
                return Result<MessageDto>.Failure("Access denied to this message");
            }

            // Get sender name
            var senderName = await GetUserDisplayNameAsync(message.SenderId, cancellationToken);

            // Map to DTO
            var dto=_mapper.Map<MessageDto>(message);
            dto=dto with { SenderName=senderName };

            return Result<MessageDto>.Success(dto);
        }
        private async Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken)
        {
            var result = await _userServiceClient.GetUserDisplayNameAsync(userId, cancellationToken);
            return result.IsSuccess && result.Data != null ? result.Data : "Unknown User";
        }
    }
}