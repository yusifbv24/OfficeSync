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
        private readonly IMapper _mapper;

        public SearchMessagesQueryHandler(
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
            var pagedQuery = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            // Execute query
            var messages=await _unitOfWork.Messages.ToListAsync(pagedQuery,cancellationToken);

            //Get sender names
            var senderIds=messages.Select(m=>m.SenderId).Distinct().ToList();
            var userNamesDict = new Dictionary<Guid, string>();
            foreach(var senderId in senderIds)
            {
                var userResult = await _userServiceClient.GetUserDisplayNameAsync(senderId, cancellationToken);
                userNamesDict[senderId] = userResult.IsSuccess && userResult.Data != null
                    ? userResult.Data
                    : "Unknown User";
            }

            // Map to DTOs
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
                AttachmentCount = m.Attachments.Count
            })
                .ToList();

            var pagedResult = PagedResult<MessageListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<MessageListDto>>.Success(pagedResult);
        }
    }
}