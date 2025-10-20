using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChannelService.Application.Commands.Channels
{
    /// <summary>
    /// Command to unarchive a channel.
    /// </summary>
    public record UnarchiveChannelCommand(
        Guid ChannelId,
        Guid UnarchivedBy):IRequest<Result<bool>>;

    public class UnarchiveChannelCommandValidator : AbstractValidator<UnarchiveChannelCommand>
    {
        public UnarchiveChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("ChannelId is required");

            RuleFor(x => x.UnarchivedBy)
                .NotEmpty().WithMessage("UnarchivedBy is required");
        }
    }

    public class UnarchiveChannelCommandHandler : IRequestHandler<UnarchiveChannelCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UnarchiveChannelCommandHandler> _logger;

        public UnarchiveChannelCommandHandler(IUnitOfWork unitOfWork, ILogger<UnarchiveChannelCommandHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }

        public async Task<Result<bool>> Handle(
            UnarchiveChannelCommand request,
            CancellationToken cancellationToken)
        {
            var channel=await _unitOfWork.Channels.GetByIdAsync(request.ChannelId,cancellationToken);
            if (channel == null)
            {
                return Result<bool>.Failure("Channel not found");
            }

            try
            {
                channel.Unarchive(request.UnarchivedBy);

                await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                        "Channel {ChannelId} unarchived by {UnarchivedBy}",
                        request.ChannelId,
                        request.UnarchivedBy);

                return Result<bool>.Success(true, "Channel unarchived successfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}