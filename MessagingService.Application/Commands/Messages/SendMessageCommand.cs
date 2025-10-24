using AutoMapper;
using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;
using MessagingService.Domain.Entities;
using MessagingService.Domain.Enums;
using MessagingService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    /// <summary>
    /// Command to send a new message to a channel.
    /// Immutable record ensures thread safety.
    /// </summary>
    public record SendMessageCommand(
        Guid ChannelId,
        Guid SenderId,
        string Content,
        MessageType Type=MessageType.Text,
        Guid? ParentMessageId=null):IRequest<Result<MessageDto>>;


    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("ChannelId is required");
            RuleFor(x => x.SenderId)
                .NotEmpty().WithMessage("SenderId is required");
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Message content is required")
                .MaximumLength(4000).WithMessage("Message content cannot exceed 4000 characters");
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid message type");
        }
    }



    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<SendMessageCommandHandler> _logger;

        public SendMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IUserServiceClient userServiceClient,
            IMapper mapper,
            ILogger<SendMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient= channelServiceClient;
            _userServiceClient= userServiceClient;
            _mapper= mapper;
            _logger= logger;
        }

        public async Task<Result<MessageDto>> Handle(
            SendMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify the user is a member of the channel
                // This is a cross-service call to Channel Service
                var isMember = await _channelServiceClient.IsUserMemberOfChannelAsync(
                    request.ChannelId,
                    request.SenderId,
                    cancellationToken);

                if (!isMember.IsSuccess || !isMember.Data)
                {
                    return Result<MessageDto>.Failure("User is not a member of this channel");
                }

                // If this is a reply,verify parent message exists
                if (request.ParentMessageId.HasValue)
                {
                    var parentExists = await _unitOfWork.Messages.ExistsAsync(
                        m => m.Id == request.ParentMessageId.Value && !m.IsDeleted,
                        cancellationToken);

                    if (!parentExists)
                    {
                        return Result<MessageDto>.Failure("Parent message not found");
                    }
                }

                // Create value object with validation
                var content = MessageContent.Create(request.Content);

                // Create message using domain model
                var message = Message.Create(
                    channelId: request.ChannelId,
                    senderId: request.SenderId,
                    content: content,
                    type: request.Type,
                    parentMessageId: request.ParentMessageId);

                // Persist to a database
                await _unitOfWork.Messages.AddAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Message {MessageId} sent to channel {ChannelId} by user {SenderId}",
                    message.Id,
                    request.ChannelId,
                    request.SenderId);

                // Get sender name for the DTO
                var senderName = await GetUserDisplayNameAsync(request.SenderId, cancellationToken);

                // Map to DTO
                var dto = _mapper.Map<MessageDto>(message);
                dto=dto with { SenderName=senderName };
                return Result<MessageDto>.Success(dto, "Message sent succesfully");
            }
            catch (ArgumentException ex)
            {
                // Validation errors from value objects
                return Result<MessageDto>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error sending message");
                return Result<MessageDto>.Failure("An error occured while sending the message");
            }
        }


        private async Task<string> GetUserDisplayNameAsync(Guid userId,CancellationToken cancellationToken)
        {
            var result=await _userServiceClient.GetUserDisplayNameAsync(userId, cancellationToken);
            return result.IsSuccess && result.Data != null ? result.Data : "Unknown User";
        }
    }
}