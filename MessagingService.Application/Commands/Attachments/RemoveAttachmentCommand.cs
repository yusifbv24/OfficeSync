using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Attachments
{
    public record RemoveAttachmentCommand(
        Guid MessageId,
        Guid AttachmentId,
        Guid RemovedBy
    ) : IRequest<Result<bool>>;



    public class RemoveAttachmentCommandValidator : AbstractValidator<RemoveAttachmentCommand>
    {
        public RemoveAttachmentCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("MessageId is required");

            RuleFor(x => x.AttachmentId)
                .NotEmpty().WithMessage("AttachmentId is required");

            RuleFor(x => x.RemovedBy)
                .NotEmpty().WithMessage("RemovedBy is required");
        }
    }   


    public class RemoveAttachmentCommandHandler : IRequestHandler<RemoveAttachmentCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveAttachmentCommandHandler> _logger;

        public RemoveAttachmentCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemoveAttachmentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            RemoveAttachmentCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Load message with attachments
                var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.MessageId,
                    cancellationToken,
                    m => m.Attachments);

                if (message == null)
                {
                    return Result<bool>.Failure("Message not found");
                }

                // Verify ownership
                if (message.SenderId != request.RemovedBy)
                {
                    return Result<bool>.Failure(
                        "Only the message sender can remove attachments");
                }

                // Find the attachment
                var attachment = message.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
                if (attachment == null)
                {
                    return Result<bool>.Failure("Attachment not found");
                }

                // Remove the attachment
                await _unitOfWork.Attachments.DeleteAsync(attachment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Attachment {AttachmentId} removed from message {MessageId} by user {RemovedBy}",
                    request.AttachmentId,
                    request.MessageId,
                    request.RemovedBy);

                return Result<bool>.Success(true, "Attachment removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing attachment");
                return Result<bool>.Failure("An error occurred while removing the attachment");
            }
        }
    }
}
