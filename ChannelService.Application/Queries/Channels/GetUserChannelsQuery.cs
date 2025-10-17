using AutoMapper;
using ChannelService.Application.Channels;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using MediatR;

namespace ChannelService.Application.Queries.Channels
{
    /// <summary>
    /// Query to get all channels a specific user is a member of
    /// </summary>
    public record GetUserChannelsQuery(
        Guid UserId,
        int PageNumber=1,
        int PageSize=20
    ): IRequest<Result<PagedResult<ChannelListDto>>>;


    public class GetUserChannelsQueryHandler:IRequestHandler<GetUserChannelsQuery, Result<PagedResult<ChannelListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetUserChannelsQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork=unitOfWork;
            _mapper=mapper;
        }

        public async Task<Result<PagedResult<ChannelListDto>>> Handle(
            GetUserChannelsQuery request,
            CancellationToken cancellationToken)
        {
            // Build query - channels where user is an active member
            var query = _unitOfWork.Channels
                .GetQueryable()
                .Where(c => c.Members.Any(m => m.UserId == request.UserId && !m.IsRemoved))
                .OrderByDescending(c => c.UpdatedAt);

            var totalCount=await query.CountAsync(cancellationToken);

            var channels = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

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