using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChannelService.Application.Commands.Members
{
    /// <summary>
    /// Command to add a member to a channel
    /// </summary>
    public record AddMemberCommand(
        Guid ChannelId,
        Guid UserId,
        Guid AddedBy,
        MemberRole Role=MemberRole.Member):IRequest<Result<bool>>;



    public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
    {
        public AddMemberCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.AddedBy)
                .NotEmpty().WithMessage("AddedBy is required");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Invalid member role");
        }
    }


    public class AddMemberCommandHandler:IRequestHandler<AddMemberCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddMemberCommandHandler> _logger;

        public AddMemberCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AddMemberCommandHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }

        public async Task<Result<bool>> Handle(
            AddMemberCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var channel = await _unitOfWork.Channels.GetByIdAsync(request.ChannelId, cancellationToken);
                if (channel==null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Failure("Channel not found");
                }

                // Use domain logic for adding member (includes all business rules)
                channel.AddMember(request.UserId,request.AddedBy,request.Role);

                await _unitOfWork.Channels.UpdateAsync(channel,cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger?.LogInformation(
                    "Member {UserId} added to channel {ChannelId} by {AddedBy}",
                    request.UserId,
                    request.ChannelId,
                    request.AddedBy);

                return Result<bool>.Success(true, "Member added succesfully");
            }
            catch (InvalidOperationException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<bool>.Failure(ex.Message);
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}