using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    public record DeleteMessageCommand(
        Guid MessageId,
        Guid DeletedBy):IRequest<Result<bool>>;



    public class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
    {
        public DeleteMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("MessageId is required");
            RuleFor(x => x.DeletedBy)
                .NotEmpty().WithMessage("DeletedBy is required");
        }
    }


    public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteMessageCommandHandler> _logger;

        public DeleteMessageCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger= logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if(message == null)
                {
                    return Result<bool>.Failure("Message not found");
                }

                // Use domain logic to delete the message
                // This enforces business rules (sender check, already deleted check)
                message.Delete(request.DeletedBy);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                "Message {MessageId} deleted by user {DeletedBy}",
                request.MessageId,
                request.DeletedBy);

                return Result<bool>.Success(true, "Message deleted successfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return Result<bool>.Failure("An error occurred while deleting the message");
            }
        }
    }
}