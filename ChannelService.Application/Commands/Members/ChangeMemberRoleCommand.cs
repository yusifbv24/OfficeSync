using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChannelService.Application.Commands.Members
{
    public record ChangeMemberRoleCommand(
        Guid ChannelId,
        Guid UserId,
        MemberRole Role,
        Guid ChangedBy):IRequest<Result<bool>>;


    public class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
    {
        public ChangeMemberRoleCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("ChannelId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid member role");

            RuleFor(x => x.ChangedBy)
                .NotEmpty().WithMessage("ChangedBy is required");
        }
    }


    public class ChangeMemberRoleCommandHandler : IRequestHandler<ChangeMemberRoleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChangeMemberRoleCommandHandler> _logger;

        public ChangeMemberRoleCommandHandler(IUnitOfWork unitOfWork, ILogger<ChangeMemberRoleCommandHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }

        public async Task<Result<bool>> Handle(
            ChangeMemberRoleCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var channel = await _unitOfWork.Channels.GetByIdWithIncludesAsync(
                    request.ChannelId,
                    cancellationToken,
                    c => c.Members);

                if (channel == null)
                {
                    return Result<bool>.Failure("Channel not found");
                }
                // Use domain logic for changing role
                // This includes all business rule validation:
                // - Verifies the person making the change is an owner
                // - Prevents changing the last owner's role
                // - Ensures the member exists in the channel
                channel.ChangeMemberRole(request.UserId, request.Role, request.ChangedBy);

                // Update the channel 
                await _unitOfWork.Channels.UpdateAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Member {UserId} role changed to {Role} in channel {ChannelId} by {ChangedBy}",
                    request.UserId,
                    request.Role,
                    request.ChannelId,
                    request.ChangedBy);

                return Result<bool>.Success(true, "Member role changed successfully");
            }
            catch (InvalidOperationException ex)
            {
                // Domain logic threw a business rule violation
                _logger?.LogWarning(
                    "Failed to change member role: {Message}. Channel: {ChannelId}, User: {UserId}",
                    ex.Message,
                    request.ChannelId,
                    request.UserId);

                return Result<bool>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Unexpected error changing member role. Channel: {ChannelId}, User: {UserId}",
                    request.ChannelId,
                    request.UserId);

                return Result<bool>.Failure("An unexpected error occurred while changing member role");
            }
        }
    }
}