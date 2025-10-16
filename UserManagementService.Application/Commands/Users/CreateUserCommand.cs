using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Interfaces;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Enums;

namespace UserManagementService.Application.Commands.Users
{
    /// <summary>
    /// Command to create a new user with both authentication credentials and profile.
    /// This command coordinates between Identity Service (credentials) and this service (profile).
    /// </summary>
    public record CreateUserCommand(
        string Username,
        string Email,
        string Password,
        string DisplayName,
        string? AvatarUrl,
        string? Notes,
        Guid CreatedBy): IRequest<Result<UserProfileDto>>;


    public class CreateUserCommandValidator: AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x=>x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long")
                .MaximumLength(30).WithMessage("Username must be at most 30 characters long")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters,numbers,underscores and hyphens");


            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");


            RuleFor(x=>x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.DisplayName)
                        .NotEmpty().WithMessage("Display name is required")
                        .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
                        .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required");
        }
    }

    public class CreateUserCommanHandler : IRequestHandler<CreateUserCommand, Result<UserProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityServiceClient _identityServiceClient;
        private readonly ILogger<CreateUserCommanHandler> _logger;


        public CreateUserCommanHandler(
            IUnitOfWork unitOfWork, 
            IIdentityServiceClient identityServiceClient, 
            ILogger<CreateUserCommanHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _identityServiceClient = identityServiceClient;
            _logger = logger;
        }


        public async Task<Result<UserProfileDto>> Handle(
            CreateUserCommand request, CancellationToken cancellationToken)
        {
            // First check if a profile already exists for any user with this display name
            var existingProfile = await _unitOfWork.UserProfiles.GetFirstOrDefaultAsync(
                    p => p.DisplayName == request.DisplayName, cancellationToken);

            if (existingProfile != null)
            {
                return Result<UserProfileDto>.Failure("A user profile with the same display name already exists.");
            }

            // Create user in Identity Service first (this creates authentication credentials)
            var identityResult = await _identityServiceClient.CreateUserAsync(
                request.Username,
                request.Email,
                request.Password,
                cancellationToken);


            if (!identityResult.IsSuccess)
            {
                _logger.LogWarning(
                "Failed to create user in Identity Service: {Username}",
                request.Username);
                return Result<UserProfileDto>.Failure(
                    "Failed to create user credentials",
                    identityResult.Errors);
            }

            // Now create the user profile in this service
            var userProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId=identityResult.Data!.UserId,
                DisplayName=request.DisplayName,
                AvatarUrl=request.AvatarUrl,
                Status=UserStatus.Active,
                CreatedBy=request.CreatedBy,
                CreatedAt=DateTime.UtcNow,
                UpdatedAt=DateTime.UtcNow,
                Notes=request.Notes
            };

            await _unitOfWork.UserProfiles.AddAsync(userProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                    "User profile created successfully: {UserId}, DisplayName: {DisplayName}",
                    userProfile.UserId,
                    userProfile.DisplayName);

            // Map to DTO
            var dto = new UserProfileDto(
                    Id: userProfile.Id,
                    UserId: userProfile.UserId,
                    DisplayName: userProfile.DisplayName,
                    AvatarUrl: userProfile.AvatarUrl,
                    Status: userProfile.Status,
                    CreatedAt: userProfile.CreatedAt,
                    UpdatedAt: userProfile.UpdatedAt,
                    LastSeenAt: userProfile.LastSeenAt,
                    CreatedBy: userProfile.CreatedBy,
                    Notes: userProfile.Notes,
                    Role: null,
                    RoleAssignedAt: null,
                    RoleAssignedBy: null,
                    Permissions: null
                );

            return Result<UserProfileDto>.Success(dto, "User created successfully");
        }
    }
}