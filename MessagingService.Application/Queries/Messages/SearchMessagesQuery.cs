using AutoMapper;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingService.Application.Queries.Messages
{
    /// <summary>
    /// Query to search messages by content within a channel.
    /// Useful for finding specific conversations or information.
    /// </summary>
    public record SearchMessagesQuery(
        Guid ChannelId,
        Guid RequestedBy,
        string SearchTerm,
        int PageNumber=1,
        int PageSize=50):IRequest<Result<PagedResult<MessageListDto>>>;



    public class SearchMessagesQueryHandler:IRequestHandler<SearchMessagesQuery, Result<PagedResult<MessageListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;

        public SearchMessagesQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient = channelServiceClient;
            _userServiceClient = userServiceClient;
        }

        public async Task<Result<PagedResult<MessageListDto>>> Handle(
            SearchMessagesQuery request,
            CancellationToken cancellationToken)
        {
            // Validate search term
            if(string.IsNullOrWhiteSpace(request.SearchTerm) || request.SearchTerm.Length < 2)
            {
                return Result<PagedResult<MessageListDto>>.Failure(
                    "Search term must be at least 2 characters");
            }

            // Verify the requesting user is a member of the channel
            var isMember = await _channelServiceClient.IsUserMemberOfChannelAsync(
                request.ChannelId,
                request.RequestedBy,
                cancellationToken);

            if(!isMember.IsSuccess || !isMember.Data)
            {
                return Result<PagedResult<MessageListDto>>.Failure("Access denied to this channel");
            }
            
            // Build search query
            var searchTermLower=request.SearchTerm.ToLower();
            var query = _unitOfWork.Messages
                .GetQueryable()
                .Where(m => m.ChannelId == request.ChannelId)
                .Where(m => !m.IsDeleted)
                .Where(m => EF.Functions.Like(m.Content.ToLower(), $"%{searchTermLower}%"));

            // Order by relevance (most recent matches first)
            query = query.OrderByDescending(m => m.CreatedAt);

            // Get total count
            var totalCount=await _unitOfWork.Messages.CountAsync(query,cancellationToken);

            // Apply pagination
            var messages = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MessageListDto
                {
                    Id = m.Id,
                    ChannelId = m.ChannelId,
                    SenderId = m.SenderId,
                    SenderName = "",
                    Content = m.Content,
                    Type = m.Type,
                    IsEdited = m.IsEdited,
                    IsDeleted = m.IsDeleted,
                    CreatedAt = m.CreatedAt,
                    ReactionCount = m.Reactions.Count(r => !r.IsRemoved),
                    AttachmentCount = m.AttachmentFields.Count
                })
                .ToListAsync();

            //Get sender names
            var senderIds= messages
                .Select(m=>m.SenderId)
                .Distinct()
                .ToList();

            Dictionary<Guid, string> userNamesDict;

            if (senderIds.Any())
            {
                var batchResult = await _userServiceClient.GetUserDisplayNamesBatchAsync(
                    senderIds, cancellationToken);

                if(batchResult.IsSuccess && batchResult.Data != null)
                {
                    userNamesDict= batchResult.Data;
                }
                else
                {
                    userNamesDict= new Dictionary<Guid, string>();
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

            var pagedResult = PagedResult<MessageListDto>.Create(
                messages,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<MessageListDto>>.Success(pagedResult);
        }
    }
}