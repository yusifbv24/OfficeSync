using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Reactions
{
    public record RemoveReactionCommand(
        Guid UserId,
        Guid MessageId,
        string Emoji
    ):IRequest<Result<bool>>;


    public class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
    {
        public RemoveReactionCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("MessageId is required");
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");
            RuleFor(x => x.Emoji)
                .NotEmpty().WithMessage("Emoji is required");
        }
    }




    public class RemoveReactionCommandHandler:IRequestHandler<RemoveReactionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveReactionCommandHandler> _logger;

        public RemoveReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemoveReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<bool>> Handle(
            RemoveReactionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.MessageId,
                    cancellationToken,
                    m => m.Reactions);

                if(message == null)
                {
                    return Result<bool>.Failure("Message not found");
                }

                // Use domain logic to remove reaction
                message.RemoveReaction(request.UserId,request.Emoji);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Reaction removed from message {MessageId} by user {UserId}",
                    request.MessageId,
                    request.UserId); 

                return Result<bool>.Success(true, "Reaction removed succesfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error removing reaction");
                return Result<bool>.Failure("An error occured while removing the reaction");
            }
        }
    }
}