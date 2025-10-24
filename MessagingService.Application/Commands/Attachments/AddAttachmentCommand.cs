using AutoMapper;
using FluentValidation;
using MediatR;
using MessagingService.Application.Attachments;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using Microsoft.Extensions.Logging;


namespace MessagingService.Application.Commands.Attachments
{
    public record AddAttachmentCommand(
        Guid MessageId,
        Guid FileId,
        string FileName,
        string FileUrl,
        long FileSize,
        string MimeType,
        Guid AddedBy
    ) : IRequest<Result<MessageAttachmentDto>>;


    public class AddAttachmentCommandValidator : AbstractValidator<AddAttachmentCommand>
    {
        public AddAttachmentCommandValidator()
        {
            RuleFor(x => x.MessageId)
                .NotEmpty().WithMessage("MessageId is required");

            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("FileId is required");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("FileName is required")
                .MaximumLength(255).WithMessage("FileName cannot exceed 255 characters");

            RuleFor(x => x.FileUrl)
                .NotEmpty().WithMessage("FileUrl is required")
                .MaximumLength(500).WithMessage("FileUrl cannot exceed 500 characters");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("FileSize must be greater than 0")
                .LessThan(104857600).WithMessage("FileSize cannot exceed 100MB"); // 100MB limit

            RuleFor(x => x.MimeType)
                .NotEmpty().WithMessage("MimeType is required")
                .MaximumLength(100).WithMessage("MimeType cannot exceed 100 characters");

            RuleFor(x => x.AddedBy)
                .NotEmpty().WithMessage("AddedBy is required");
        }
    }

    public class AddAttachmentCommandHandler : IRequestHandler<AddAttachmentCommand, Result<MessageAttachmentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddAttachmentCommandHandler> _logger;

        public AddAttachmentCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddAttachmentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<MessageAttachmentDto>> Handle(
            AddAttachmentCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Load message with existing attachments
                var message = await _unitOfWork.Messages.GetByIdWithIncludesAsync(
                    request.MessageId,
                    cancellationToken,
                    m => m.Attachments);

                if (message == null)
                {
                    return Result<MessageAttachmentDto>.Failure("Message not found");
                }

                // Verify that the user adding the attachment is the message sender
                // This prevents users from attaching files to other people's messages
                if (message.SenderId != request.AddedBy)
                {
                    return Result<MessageAttachmentDto>.Failure(
                        "Only the message sender can add attachments");
                }

                // Business rule: Limit attachments per message
                if (message.Attachments.Count >= 10)
                {
                    return Result<MessageAttachmentDto>.Failure(
                        "Maximum 10 attachments per message");
                }

                // Use domain logic to add attachment
                // This will validate and update the message type if needed
                message.AddAttachment(
                    fileId: request.FileId,
                    fileName: request.FileName,
                    fileUrl: request.FileUrl,
                    fileSize: request.FileSize,
                    mimeType: request.MimeType);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Attachment {FileId} added to message {MessageId} by user {AddedBy}",
                    request.FileId,
                    request.MessageId,
                    request.AddedBy);

                // Get the newly added attachment and map to DTO
                var attachment = message.Attachments.Last();
                var dto = _mapper.Map<MessageAttachmentDto>(attachment);

                return Result<MessageAttachmentDto>.Success(dto, "Attachment added successfully");
            }
            catch (InvalidOperationException ex)
            {
                return Result<MessageAttachmentDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attachment");
                return Result<MessageAttachmentDto>.Failure("An error occurred while adding the attachment");
            }
        }
    }
}
