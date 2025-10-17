using AutoMapper;
using ChannelService.Application.Channels;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Enums;
using ChannelService.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ChannelService.Application.Commands.Channels
{
    public record UpdateChannelCommand(
        Guid ChannelId,
        string? Name,
        string? Description,
        Guid UpdatedBy):IRequest<Result<ChannelDto>>;


    public class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
    {
        public UpdateChannelCommandValidator()
        {
            When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .MinimumLength(2).WithMessage("Channel must be at least 2 characters")
                    .MaximumLength(100).WithMessage("Channel name cannot exceed 100 characters");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
            });
        }
    }



    public class UpdateChannelCommandHandler : IRequestHandler<UpdateChannelCommand, Result<ChannelDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpdateChannelCommandHandler(IUnitOfWork unitOfWork,IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        public async Task<Result<ChannelDto>> Handle(
            UpdateChannelCommand request,
            CancellationToken cancellationToken)
        {
            var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken);
            if (channel == null)
                return Result<ChannelDto>.Failure("Channel not found");

            // Verify user has permission (owner only)
            var role = channel.GetMemberRole(request.UpdatedBy);
            if (role != MemberRole.Owner)
                return Result<ChannelDto>.Failure("Only channel owners can update channel");

            // Update using domain logic
            ChannelName? name = null;
            if (!string.IsNullOrWhiteSpace(request.Name))
                name = ChannelName.Create(request.Name);

            channel.UpdateInfo(name, request.Description);

            await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<ChannelDto>(channel);

            return Result<ChannelDto>.Success(dto, "Channel updated succesfully");
        }
    }
}