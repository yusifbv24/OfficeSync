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
    /// Query to get a channel by ID.
    /// </summary>
    public record GetChannelByIdQuery(
        Guid ChannelId,
        Guid RequestedBy
    ):IRequest<Result<ChannelDto>>;



    public class GetChannelByIdQueryHandler:IRequestHandler<GetChannelByIdQuery, Result<ChannelDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetChannelByIdQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork=unitOfWork;
            _mapper=mapper;
        }

        public async Task<Result<ChannelDto>> Handle(
            GetChannelByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Use IQueryable to build query with deferred execution
            var query = _unitOfWork.Channels
                .GetQueryable()
                .Where(c => c.Id == request.ChannelId);

            var channel = await _unitOfWork.Channels.FirstOrDefaultAsync(query, cancellationToken);

            if (channel == null)
                return Result<ChannelDto>.Failure("Channel not found");

            // Check if user has access to this channel
            if (channel.Type == ChannelType.Private)
            {
                if (!channel.IsMember(request.RequestedBy))
                    return Result<ChannelDto>.Failure("Access denied to private channel");
            }

            var dto=_mapper.Map<ChannelDto>(channel);
            return Result<ChannelDto>.Success(dto);
        }
    }
}