using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    /// <summary>
    /// Command to mark a message as read by a user.
    /// This creates or updates a read receipt.
    /// </summary>
    public record MarkMessageAsReadCommand(
        Guid MessageId,
        Guid UserId
    ):IRequest<Result<bool>>;




    public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkMessageAsReadCommandHandler> _logger;

        public MarkMessageAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<MarkMessageAsReadCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<bool>> Handle(
            MarkMessageAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.MessageId,
                    cancellationToken,
                    m => m.ReadReceipts);

                if( message == null )
                {
                    return Result<bool>.Failure("Message not found");
                }

                // Use domain logic to mark as read
                message.MarkAsRead(request.UserId);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Message {MessageId} marked as read by user {UserId}",
                    request.MessageId,
                    request.UserId);

                return Result<bool>.Success(true, "Message marked as read");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking message as read");
                return Result<bool>.Failure("An error occured while marking message as read");
            }
        }
    }
}