using AutoMapper;
using ChannelService.Application.Channels;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChannelService.Application.Queries.Channels
{
    /// <summary>
    /// Query to get all channels with pagination.
    /// </summary>
    public record GetAllChannelsQuery(
        Guid RequestedBy,
        int PageNumber=1,
        int PageSize=20,
        bool IncludeArchived=false
    ):IRequest<Result<PagedResult<ChannelListDto>>>;


    public class GetAllChannelsQueryHandler:IRequestHandler<GetAllChannelsQuery, Result<PagedResult<ChannelListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllChannelsQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork=unitOfWork;
            _mapper=mapper;
        }

        public async Task<Result<PagedResult<ChannelListDto>>> Handle(
            GetAllChannelsQuery request,
            CancellationToken cancellationToken)
        {
            // Build query using IQueryable - no database hit yet
            var query = _unitOfWork.Channels.GetQueryable();


            // Filter archived channels if not included
            if (!request.IncludeArchived)
            {
                query = query.Where(c => !c.IsArchived);
            }

            // User can see all public channels and private channels they are member of
            query = query.Where(c =>
                c.Type == ChannelType.Public ||
                (c.Type == ChannelType.Private && c.Members.Any(m => m.UserId == request.RequestedBy && !m.IsRemoved)));


            // Order by creation date
            query = query.OrderByDescending(c => c.CreatedAt);

            // Get total count - single database query
            var totalCount = await _unitOfWork.Channels.CountAsync(query, cancellationToken);

            // Apply pagination
            var pagedQuery = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            // Execute query - Now the database is hit with optimized SQL
            var channels = await _unitOfWork.Channels.ToListAsync(pagedQuery,cancellationToken);

            var dtos = _mapper.Map<List<ChannelListDto>>(channels);

            var pagedResult = PagedResult<ChannelListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<ChannelListDto>>.Success(pagedResult);
        }
    }
}