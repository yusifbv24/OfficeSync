using ChannelService.Application.Channels;
using ChannelService.Application.Common;
using ChannelService.Application.Interfaces;
using ChannelService.Domain.Entities;
using ChannelService.Domain.Enums;
using ChannelService.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChannelService.Application.Commands.Channels
{
    public record CreateChannelCommand(
        string Name,
        string? Description,
        ChannelType Type,
        Guid CreatedBy):IRequest<Result<ChannelDto>>;



    public class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
    {
        public CreateChannelCommandValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Channel name is required")
               .MinimumLength(2).WithMessage("Channel name must be at least 2 characters")
               .MaximumLength(100).WithMessage("Channel name cannot exceed 100 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid channel type");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
        }
    }



    public class CreateChannelCommandHandler:IRequestHandler<CreateChannelCommand, Result<ChannelDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateChannelCommandHandler> _logger;

        public CreateChannelCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateChannelCommandHandler> logger)
        {
            _unitOfWork=unitOfWork;
            _logger=logger;
        }

        public async Task<Result<ChannelDto>> Handle(
            CreateChannelCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Start transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Create value object with validation
                var channelName = ChannelName.Create(request.Name);

                // Check if channel name already exists
                var nameExists = await _unitOfWork.Channels.ExistsAsync(
                    c => c.Name == channelName.Value && !c.IsArchived,
                    cancellationToken);

                if (nameExists)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ChannelDto>.Failure("Channel name already exists");
                }

                // Create channel using domain model
                var channel = Channel.Create(
                    name: channelName,
                    type: request.Type,
                    createdBy: request.CreatedBy,
                    description: request.Description);

                await _unitOfWork.Channels.AddAsync(channel, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Commit transaction
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger?.LogInformation(
                        "Channel created: {ChannelId}, Name: {Name}, Type: {Type}",
                        channel.Id,
                        channel.Name,
                        channel.Type);


                // Map to DTO
                var dto = new ChannelDto(
                    Id: channel.Id,
                    Name: channel.Name,
                    Description: channel.Description,
                    Type: channel.Type,
                    IsArchived: channel.IsArchived,
                    CreatedBy: channel.CreatedBy,
                    CreatedAt: channel.CreatedAt,
                    UpdatedAt: channel.UpdatedAt,
                    MemberCount: channel.Members.Count);


                return Result<ChannelDto>.Success(dto, "Channel created succesfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger?.LogError(ex, "Error creating channel");
                return Result<ChannelDto>.Failure("An error occurred while creating channel");
            }
        }
    }
}