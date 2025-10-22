using AutoMapper;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;

namespace MessagingService.Application.Queries.Messages
{
    /// <summary>
    /// Query to get paginated messages from a channel.
    /// Ordered by creation time (newest first by default).
    /// </summary>
    public record GetChannelMessagesQuery(
        Guid ChannelId,
        Guid RequestedBy,
        int PageNumber=1,
        int PageSize=50,
        bool IncludeDeleted=false):IRequest<Result<PagedResult<MessageListDto>>>;



    public class GetChannelMessageQueryHandler:IRequestHandler<GetChannelMessagesQuery, Result<PagedResult<MessageListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;

        public GetChannelMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient = channelServiceClient;
            _userServiceClient = userServiceClient;
            _mapper = mapper;
        }


        public async Task<Result<PagedResult<MessageListDto>>> Handle(
            GetChannelMessagesQuery request,
            CancellationToken cancellationToken)
        {
            // Verify the requesting user is a member of the channel
            var isMember=await _channelServiceClient.IsUserMemberOfChannelAsync(
                request.ChannelId,
                request.RequestedBy,
                cancellationToken);

            if(!isMember.IsSuccess || !isMember.Data)
            {
                return Result<PagedResult<MessageListDto>>.Failure("Access denied to this channel");
            }

            // Build query using IQueryable - deferred execution
            var query = _unitOfWork.Messages
                .GetQueryable()
                .Where(m => m.ChannelId == request.ChannelId);

            // Optionally filter deleted messages
            if (!request.IncludeDeleted)
            {
                query=query.Where(m=>!m.IsDeleted);
            }

            // Order by creation time (newest first for chat applications)
            query = query.OrderByDescending(m => m.CreatedAt);

            // Get total count - this executes a COUNT query
            var totalCount=await _unitOfWork.Messages.CountAsync(query,cancellationToken);

            // Apply pagination - this modifies the query to use OFFSET/FETCH
            var pagedQuery= query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            // Execute query - now we hit the database with optimized SQL
            var messages = await _unitOfWork.Messages.ToListAsync(pagedQuery, cancellationToken);

            // Get uniqiue sender IDs to batch-fetch user names
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            // Create a dictionary of sender names for efficient lookup
            var userNamesDict = new Dictionary<Guid, string>();
            foreach(var senderId in senderIds)
            {
                var userResult = _userServiceClient.GetUserDisplayNameAsync(senderId, cancellationToken).Result();
                userNamesDict[senderId] = userResult.IsSuccess && userResult.Data != null
                    ? userResult.Data
                    : "Unknown User";
            }

            // Map to DTOs with sender names
            var dtos = messages.Select(m => new MessageListDto
            {
                Id = m.Id,
                ChannelId = m.ChannelId,
                SenderId = m.SenderId,
                SenderName = userNamesDict.GetValueOrDefault(m.SenderId, "Unknown User"),
                Content = m.Content,
                Type = m.Type,
                IsEdited = m.IsEdited,
                IsDeleted = m.IsDeleted,
                CreatedAt = m.CreatedAt,
                ReactionCount = m.Reactions.Count(r => !r.IsRemoved),
                AttachmentCount=m.Attachments.Count
            })
                .ToList();

            // Create paged result
            var pagedResult = PagedResult<MessageListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<MessageListDto>>.Success(pagedResult);
        }
    }
}