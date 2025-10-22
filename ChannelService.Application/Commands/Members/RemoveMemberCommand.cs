using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChannelService.Application.Commands.Members
{
    /// <summary>
    /// Command to remove a member from a channel.
    /// </summary>
    public record RemoveMemberCommand(
        Guid ChannelId,
        Guid UserId,
        Guid RemovedBy):IRequest<Result<bool>>;

    public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
    {
        public RemoveMemberCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("ChannelId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.RemovedBy)
                .NotEmpty().WithMessage("RemovedBy is required");
        }
    }


    public class RemoveMemberCommandHandler:IRequestHandler<RemoveMemberCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveMemberCommandHandler> _logger;
        public RemoveMemberCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemoveMemberCommandHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }


        public async Task<Result<bool>> Handle(
            RemoveMemberCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var channel = await _unitOfWork.Channels.GetByIdWithIncludesAsync(
                    request.ChannelId, 
                    cancellationToken,
                    c=>c.Members);

                if (channel == null)
                    return Result<bool>.Failure("Channel not found");

                // Use domain logic for removing member (includes permission checks)
                channel.RemoveMember(request.UserId, request.RemovedBy);
                await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Member {UserId} removed from channel {ChannelId} by {RemovedBy}",
                    request.UserId,
                    request.ChannelId,
                    request.RemovedBy);

                return Result<bool>.Success(true, "Member removed succesfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}