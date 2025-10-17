using AutoMapper;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Application.Members;
using MediatR;

namespace ChannelService.Application.Queries.Members
{
    public record GetChannelMembersQuery(
        Guid ChannelId,
        Guid RequestedBy
    ): IRequest<Result<List<ChannelMemberDto>>>;


    public class GetChannelMembersQueryHandler:IRequestHandler<GetChannelMembersQuery, Result<List<ChannelMemberDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetChannelMembersQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        public async Task<Result<List<ChannelMemberDto>>> Handle(
            GetChannelMembersQuery request,
            CancellationToken cancellationToken)
        {
            var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken);
            if (channel == null)
                return Result<List<ChannelMemberDto>>.Failure("Channel not found");

            // Verify requestor has access to see members
            if (!channel.IsMember(request.RequestedBy))
                return Result<List<ChannelMemberDto>>.Failure("Access denied");

            // Get active members using IQueryable
            var members = await _unitOfWork.ChannelMembers
                .GetQueryable()
                .Where(m => m.ChannelId == request.ChannelId && !m.IsRemoved)
                .OrderBy(m => m.JoinedAt)
                .ToListAsync(cancellationToken);

            var dtos = _mapper.Map<List<ChannelMemberDto>>(members);

            return Result<List<ChannelMemberDto>>.Success(dtos);
        }
    }
}