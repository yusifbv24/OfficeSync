using ChannelService.Application.Channels;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ChannelService.Application.Commands.Channels
{
    /// <summary>
    /// Command to archive channel
    /// </summary>
    public record ArchiveChannelCommand(
        Guid ChannelId,
        Guid ArchivedBy):IRequest<Result<bool>>;


    public class ArchiveChannelCommandValidator : AbstractValidator<ArchiveChannelCommand>
    {
        public ArchiveChannelCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("ChannelId is required");

            RuleFor(x => x.ArchivedBy)
                .NotEmpty().WithMessage("ArchivedBy is required");
        }
    }


    public class ArchiveChannelCommandHandler: IRequestHandler<ArchiveChannelCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ArchiveChannelCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(
            ArchiveChannelCommand request,
            CancellationToken cancellationToken)
        {
            var channel = await _unitOfWork.Channels.GetByIdWithIncludesAsync(
                request.ChannelId,
                cancellationToken,
                c => c.Members);

            if (channel == null)
                return Result<bool>.Failure("Channel not found");

            try
            {
                // Use domain logic for archiving (includes permission check)
                channel.Archive(request.ArchivedBy);

                await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true,"Channel archived succesfully");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}