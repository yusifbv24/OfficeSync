using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Reactions
{
    public record AddReactionCommand(
        Guid MessageId,
        Guid UserId,
        string Emoji):IRequest<Result<bool>>;


    public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
    {
        public AddReactionCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("MessageId is required");
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");
            RuleFor(x => x.Emoji)
                .NotEmpty().WithMessage("Emoji is required")
                .MaximumLength(10).WithMessage("Emoji cannot exceed 10 characters");
        }
    }



    public class AddReactionCommandHandler:IRequestHandler<AddReactionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddReactionCommandHandler> _logger;

        public AddReactionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AddReactionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger= logger;
        }

        public async Task<Result<bool>> Handle(
            AddReactionCommand request,
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

                // Use domain logic to add reaction
                // This enforces business rules (no duplicate reactions,not deleted,etc.)
                message.AddReaction(request.UserId,request.Emoji);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Reaction {Emoji} added to message {MessageId} by user {UserId} ",
                    request.Emoji,
                    request.MessageId,
                    request.UserId);

                return Result<bool>.Success(true, "Reaction added succesfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error adding reaction");
                return Result<bool>.Failure("An error occured while adding the reaction");
            }
        }
    }
}