using AutoMapper;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;
using MessagingService.Domain.Entities;
using MessagingService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    /// <summary>
    /// Command to forward a message to another channel.
    /// Creates a copy of the original message with a reference.
    /// </summary>
    public record ForwardMessageCommand(
        Guid OriginalMessageId,
        Guid TargetChannelId,
        Guid ForwardedBy,
        string? AdditionalComment=null
    ):IRequest<Result<MessageDto>>;




    public class ForwardMessageCommandHandler:IRequestHandler<ForwardMessageCommand, Result<MessageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<ForwardMessageCommandHandler> _logger;

        public ForwardMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient,
            IMapper mapper,
            ILogger<ForwardMessageCommandHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _channelServiceClient=channelServiceClient;
            _userServiceClient=userServiceClient;
            _mapper=mapper;
            _logger=logger;
        }


        public async Task<Result<MessageDto>> Handle(
            ForwardMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify user can access original message
                var originalMessage = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.OriginalMessageId,
                    cancellationToken,
                    m => m.AttachmentFields);

                if (originalMessage == null)
                {
                    return Result<MessageDto>.Failure("Original message not found");
                }

                // Verify user is member of target channel
                var isMember = await _channelServiceClient.IsUserMemberOfChannelAsync(
                    request.TargetChannelId,
                    request.ForwardedBy,
                    cancellationToken);

                if(!isMember.IsSuccess|| !isMember.Data)
                {
                    return Result<MessageDto>.Failure("You are not a member of the target channel");
                }

                // Build forwarded message content
                var content = string.IsNullOrWhiteSpace(request.AdditionalComment)
                    ? $"[Forwarded message]\n{originalMessage.Content}"
                    : $"{request.AdditionalComment}\n\n[Forwarded message]\n{originalMessage.Content}";

                var messageContent=MessageContent.Create(content);


                // Create new messages
                var forwardedMessage = Message.Create(
                    channelId: request.TargetChannelId,
                    senderId: request.ForwardedBy,
                    content: messageContent,
                    type: originalMessage.Type);

                await _unitOfWork.Messages.AddAsync(forwardedMessage, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Message {OriginalMessageId} forwarded to channel {TargetChannelId} by user {ForwardedBy}",
                    request.OriginalMessageId,
                    request.TargetChannelId,
                    request.ForwardedBy);

                var senderName = await GetUserDisplayNameAsync(request.ForwardedBy, cancellationToken);
                var dto = _mapper.Map<MessageDto>(forwardedMessage);
                dto = dto with { SenderName = senderName };

                return Result<MessageDto>.Success(dto, "Message forwarded succesfully");
            }
            catch (ArgumentException ex)
            {
                return Result<MessageDto>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error forwarding message");
                return Result<MessageDto>.Failure("An error occurred while forwarding the message");
            }
        }
        private async Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken)
        {
            var result = await _userServiceClient.GetUserDisplayNameAsync(userId, cancellationToken);
            return result.IsSuccess && result.Data != null ? result.Data : "Unknown User";
        }
    }
}