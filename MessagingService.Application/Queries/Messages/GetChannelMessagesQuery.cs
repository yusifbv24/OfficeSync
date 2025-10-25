using AutoMapper;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;
using Microsoft.EntityFrameworkCore;

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



    public class GetChannelMessagesQueryHandler:IRequestHandler<GetChannelMessagesQuery, Result<PagedResult<MessageListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;

        public GetChannelMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient = channelServiceClient;
            _userServiceClient = userServiceClient;
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

            // Execute query - now we hit the database with optimized SQL
            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MessageListDto
                {
                    Id = m.Id,
                    ChannelId = m.ChannelId,
                    SenderId = m.SenderId,
                    SenderName = "", // We'll fill this in next step
                    Content = m.Content,
                    Type = m.Type,
                    IsEdited = m.IsEdited,
                    IsDeleted = m.IsDeleted,
                    CreatedAt = m.CreatedAt,
                    ReactionCount = m.Reactions.Count(r => !r.IsRemoved),
                    AttachmentCount = m.Attachments.Count
                })
                .ToListAsync(cancellationToken);

            // Get uniqiue sender IDs to batch-fetch user names
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            Dictionary<Guid, string> userNamesDict;

            if(senderIds.Any())
            {
                var batchResult = await _userServiceClient.GetUserDisplayNamesBatchAsync(
                    senderIds, cancellationToken);

                if(batchResult.IsSuccess&& batchResult.Data != null)
                {
                    userNamesDict= batchResult.Data;
                }
                else
                {
                    userNamesDict = new Dictionary<Guid, string>();
                }
            }
            else
            {
                userNamesDict = new Dictionary<Guid, string>();
            }

            foreach (var message in messages)
            {
                message.SenderName = userNamesDict.TryGetValue(message.SenderId, out var name)
                    ? name
                    : "Unknown User";
            }

            // Create paged result
            var pagedResult = PagedResult<MessageListDto>.Create(
                messages,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<MessageListDto>>.Success(pagedResult);
        }
    }
}