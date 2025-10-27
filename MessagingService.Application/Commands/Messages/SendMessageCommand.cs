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
        Guid? ParentMessageId=null,
        List<Guid>? AttachmentFileIds=null
    ):IRequest<Result<MessageDto>>;


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

            // Validate file IDs if provided
            RuleFor(x => x.AttachmentFileIds)
                .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
                .WithMessage("All file IDs must be valid")
                .Must(ids => ids == null || ids.Count <= 10)
                .WithMessage("Maximum 10 files per message");
        }
    }



    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IFileServiceClient _fileServiceClient;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<SendMessageCommandHandler> _logger;

        public SendMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient,
            IFileServiceClient fileServiceClient,
            IUserServiceClient userServiceClient,
            IMapper mapper,
            ILogger<SendMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient= channelServiceClient;
            _fileServiceClient= fileServiceClient;
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
                // Step 1: Verify user is a member of the channel
                var isMember = await _channelServiceClient.IsUserMemberOfChannelAsync(
                    request.ChannelId,
                    request.SenderId,
                    cancellationToken);

                if (!isMember.IsSuccess || !isMember.Data)
                {
                    _logger?.LogWarning(
                            "User {UserId} attempted to send message to channel {ChannelId} but is not a member",
                            request.SenderId,
                            request.ChannelId);

                    return Result<MessageDto>.Failure("You are not a member of this channel");
                }

                // Step 2: If this is a reply, verify parent message exists
                if (request.ParentMessageId.HasValue)
                {
                    var parentExists = await _unitOfWork.Messages.ExistsAsync(
                        m => m.Id == request.ParentMessageId.Value && !m.IsDeleted,
                        cancellationToken);

                    if (!parentExists)
                    {
                        _logger?.LogWarning(
                            "User {UserId} attempted to reply to non-existent message {ParentMessageId}",
                            request.SenderId,
                            request.ParentMessageId);
                        return Result<MessageDto>.Failure("Parent message not found");
                    }
                }

                // Step 3: Validate file attachments
                // Files must already exist in FileService and user must have access
                List<Guid>? validateFileIds = null;
                if(request.AttachmentFileIds!=null && request.AttachmentFileIds.Any())
                {
                    _logger.LogDebug(
                        "Validating {Count} file attachments for message",
                        request.AttachmentFileIds.Count);

                    var fileValidation = await _fileServiceClient.ValidateFileAccessBatchAsync(
                        request.AttachmentFileIds,
                        request.SenderId,
                        cancellationToken);

                    if (!fileValidation.IsSuccess)
                    {
                        _logger.LogError(
                            "File validation failed for user {UserId}:{Error}",
                            request.SenderId,
                            fileValidation.Message);

                        return Result<MessageDto>.Failure(
                            "Unable to validate file attachments. Please ensure all files exist and you have access. ");
                    }

                    validateFileIds = fileValidation.Data;

                    // Check if any files were rejected
                    if (validateFileIds != null)
                    {
                        var rejectedFiles = request.AttachmentFileIds
                        .Except(validateFileIds)
                        .ToList();

                        if (rejectedFiles.Any())
                        {
                            _logger.LogWarning(
                                "User {UserId} attempted to attach files they dont have access to : {FileIds}",
                                request.SenderId,
                                string.Join(",", rejectedFiles));
                            return Result<MessageDto>.Failure(
                                $"You don't have access to {rejectedFiles.Count} of the attached files. " +
                                "Please remove them and try again.");
                        }
                    }
                }


                // Step 4: Create message content with validation
                var content = MessageContent.Create(request.Content);

                // Step 5: Create the message entity
                var message = Message.Create(
                    channelId: request.ChannelId,
                    senderId: request.SenderId,
                    content: content,
                    type: request.Type,
                    parentMessageId: request.ParentMessageId,
                    attachmentFileIds: validateFileIds);

                // Step 6: Persist to a database
                await _unitOfWork.Messages.AddAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);


                _logger?.LogInformation(
                    "Message {MessageId} sent to channel {ChannelId} by user {SenderId} with {FileCount} attachments",
                    message.Id,
                    request.ChannelId,
                    request.SenderId,
                    message.GetAttachmentCount());

                // Get sender name for the DTO
                var dto = await BuildMessageDtoAsync(message, request.SenderId, cancellationToken);

                return Result<MessageDto>.Success(dto, "Message sent succesfully");
            }
            catch (ArgumentException ex)
            {
                // Validation errors from value objects
                _logger.LogWarning(ex, "Validation error sending message");
                return Result<MessageDto>.Failure(ex.Message);
            }
            catch (Exception ex)
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



        private async Task<MessageDto> BuildMessageDtoAsync(
            Message message,
            Guid requestedBy,
            CancellationToken cancellationToken)
        {
            // Get sender name
            var senderName = await GetUserDisplayNameAsync(message.SenderId, cancellationToken);

            // Map basic message data
            var dto = _mapper.Map<MessageDto>(message);
            dto = dto with { SenderName = senderName };

            // Fetch file details from File Service if message has attachments
            if (message.GetAttachmentCount() > 0)
            {
                var fileDetailsResult = await _fileServiceClient.GetFileDetailsBatchAsync(
                    message.AttachmentFields.ToList(),
                    requestedBy,
                    cancellationToken);

                if (fileDetailsResult.IsSuccess && fileDetailsResult.Data != null)
                {
                    // Convert dictionary to list, preserving order of FileIds
                    var fileDetails = message.AttachmentFields
                        .Select(fileId => fileDetailsResult.Data.GetValueOrDefault(fileId))
                        .Where(details => details != null)
                        .ToList();

                    dto = dto with { Attachments = fileDetails! };
                }
                else
                {
                    _logger?.LogWarning(
                        "Failed to fetch file details for message {MessageId} : {Error}",
                        message.Id,
                        fileDetailsResult.Message);

                    // Still return the message, just without file details
                    dto = dto with { Attachments = [] };
                }
            }
            return dto;
        }
    }
}