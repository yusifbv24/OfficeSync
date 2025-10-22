using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

                var channel = await _unitOfWork.Channels.GetByIdWithIncludesAsync(
                    request.ChannelId,
                    cancellationToken,
                    c => c.Members);

                if (channel == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<bool>.Failure("Channel not found");
                }
                // Check if this user was previously a member (to distinguish restore from new add)
                var existingMember =channel.Members.FirstOrDefault(m=>m.UserId==request.UserId);
                var isRejoining = existingMember != null && existingMember.IsRemoved;

                // Use domain logic for adding member
                channel.AddMember(request.UserId, request.AddedBy, request.Role);

                // Get the member after the domain operation
                var member = channel.Members.FirstOrDefault(m => m.UserId == request.UserId && !m.IsRemoved);
                if (member != null)
                {
                    var context=_unitOfWork.GetContext();

                    if (isRejoining)
                    {
                        // This is a restored member - mark as modified
                        // EF needs to know we changed an existing entity
                        context.Entry(member).State = EntityState.Modified;

                        _logger?.LogInformation(
                            "Member {UserId} restored to channel {ChannelId} by {AddedBy}",
                            request.UserId,
                            request.ChannelId,
                            request.AddedBy);
                    }
                    else
                    {
                        // This is a truly new member - mark as added
                        context.Entry(member).State =EntityState.Added;

                        _logger?.LogInformation(
                            "New member {UserId} added to channel {ChannelId} by {AddedBy}",
                            request.UserId,
                            request.ChannelId,
                            request.AddedBy);
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);


                return Result<bool>.Success(true,
                    isRejoining? "Member restored succesfully" : "Member added successfully");
            }
            catch (InvalidOperationException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<bool>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error adding member: {Message}", ex.Message);
                return Result<bool>.Failure(ex.Message);
            }
        }
    }
}