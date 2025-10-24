using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
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
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.MessageId,
                    cancellationToken,
                    m => m.Reactions);

                if(message == null)
                {
                    return Result<bool>.Failure("Message not found");
                }

                var existingRemovedReaction = message.Reactions.FirstOrDefault(r =>
                    r.UserId == request.UserId &&
                    r.Emoji == request.Emoji &&
                    r.IsRemoved);

                var isRestoring = existingRemovedReaction != null;

                // Use domain logic to add reaction
                message.AddReaction(request.UserId,request.Emoji);

                var reaction = message.Reactions.FirstOrDefault(r =>
                    r.UserId == request.UserId &&
                    r.Emoji == request.Emoji &&
                    !r.IsRemoved);

                if(reaction != null)
                {
                    var context = _unitOfWork.GetContext();

                    if (isRestoring)
                    {
                        context.Entry(reaction).State = EntityState.Modified;

                        _logger?.LogInformation(
                            "Reaction {Emoji} restored on message {MessageId} by user {UserId}",
                            request.Emoji,
                            request.MessageId,
                            request.UserId);
                    }
                    else
                    {
                        context.Entry(reaction).State = EntityState.Added;

                        _logger?.LogInformation(
                            "New reaction {Emoji} added to message {MessageId} by user {UserId}",
                            request.Emoji,
                            request.MessageId,
                            request.UserId);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<bool>.Success(true,
                            isRestoring ? "Reaction restored successfully" : "Reaction added successfully");
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