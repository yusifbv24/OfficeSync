using AutoMapper;
using FluentValidation;
using MediatR;
using MessagingService.Application.Common;
using MessagingService.Application.Interfaces;
using MessagingService.Application.Messages;
using MessagingService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MessagingService.Application.Commands.Messages
{
    public record EditMessageCommand(
        Guid MessageId,
        Guid EditedBy,
        string NewContent):IRequest<Result<MessageDto>>;


    public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
    {
        public EditMessageCommandValidator()
        {
            RuleFor(x => x.MessageId)
               .NotEmpty().WithMessage("MessageId is required");
            RuleFor(x => x.EditedBy)
                .NotEmpty().WithMessage("EditedBy is required");
            RuleFor(x => x.NewContent)
                .NotEmpty().WithMessage("Message content is required")
                .MaximumLength(4000).WithMessage("Message content cannot exceed 4000 characters");
        }
    }



    public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, Result<MessageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;
        private readonly ILogger<EditMessageCommandHandler> _logger;

        public EditMessageCommandHandler(
            IUnitOfWork unitOfWork,
            IUserServiceClient userServiceClient,
            IMapper mapper,
            ILogger<EditMessageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _userServiceClient=userServiceClient;
            _mapper=mapper;
            _logger=logger;
        }


        public async Task<Result<MessageDto>> Handle(
            EditMessageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(
                    request.MessageId,
                    cancellationToken);

                if(message == null)
                {
                    return Result<MessageDto>.Failure("Message not found");
                }

                // Create value object with validation
                var content = MessageContent.Create(request.NewContent);

                // Use domain logic to edit the message
                // This enforces all business rules (sender check, deleted check, etc.)
                message.Edit(content, request.EditedBy);

                await _unitOfWork.Messages.UpdateAsync(message, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Message {MessageId} edited by user {EditedBy}",
                    request.MessageId,
                    request.EditedBy);

                // Get sender name for the DTO
                var senderName = await GetUserDisplayNameAsync(message.SenderId, cancellationToken);

                var dto=_mapper.Map<MessageDto>(message);
                dto =dto with { SenderName=senderName };
                return Result<MessageDto>.Success(dto, "Message edited succesfully");
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violations from domain
                return Result<MessageDto>.Failure(ex.Message);
            }
            catch(ArgumentException ex)
            {
                // Validation errors from value objects
                return Result<MessageDto>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error editing message");
                return Result<MessageDto>.Failure("An error occurred while editing the message");
            }
        }

        private async Task<string> GetUserDisplayNameAsync(Guid userId,CancellationToken cancellationToken)
        {
            var result= await _userServiceClient.GetUserDisplayNameAsync(userId, cancellationToken);
            return result.IsSuccess && result.Data != null ? result.Data : "Unknows user";
        }
    }
}