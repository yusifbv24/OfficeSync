using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    public record MarkChannelMessagesAsReadCommand(
        Guid ChannelId,
        Guid UserId,
        DateTime? ReadUpTo=null // If null, marks all messages are read
    ):IRequest<Result<int>>; // Returns count of messages marked



    public class MarkChannelMessagesAsReadCommandHandler:IRequestHandler<MarkChannelMessagesAsReadCommand, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkChannelMessagesAsReadCommandHandler> _logger;

        public MarkChannelMessagesAsReadCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<MarkChannelMessagesAsReadCommandHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }


        public async Task<Result<int>> Handle(
            MarkChannelMessagesAsReadCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Get all unread messages in the channel
                var query = _unitOfWork.Messages
                    .GetQueryable()
                    .Include(m => m.ReadReceipts)
                    .Where(m => m.ChannelId == request.ChannelId)
                    .Where(m => m.SenderId != request.UserId) // Don't mark own messages
                    .Where(m => !m.IsDeleted)
                    .Where(m => !m.ReadReceipts.Any(r => r.UserId == request.UserId));

                // Optionally filter by timestamp 
                if (request.ReadUpTo.HasValue)
                {
                    query = query.Where(m => m.CreatedAt <= request.ReadUpTo.Value);
                }

                var unreadMessages = await _unitOfWork.Messages.ToListAsync(query, cancellationToken);

                // If there are no unread messages, we can return early
                if (!unreadMessages.Any())
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    return Result<int>.Success(0, "No unread messages to mark");
                }

                foreach(var message in unreadMessages)
                {
                    var context = _unitOfWork.GetContext();
                    message.MarkAsRead(request.UserId);
                    var readReceipt = message.ReadReceipts.FirstOrDefault(r => r.UserId == request.UserId);
                    if (readReceipt != null)
                    {
                        context.Entry(readReceipt).State = EntityState.Added;
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Marked {Count} messages as read in channel {ChannelId} by user {UserId}",
                    unreadMessages.Count,
                    request.ChannelId,
                    request.UserId);

                return Result<int>.Success(unreadMessages.Count, $"{unreadMessages.Count} messages marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking channel messages as read");
                return Result<int>.Failure("An error occurred while marking messages as read");
            }
        }
    }
}