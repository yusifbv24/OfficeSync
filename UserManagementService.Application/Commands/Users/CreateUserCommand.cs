using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using UserManagementService.Application.Common;
using UserManagementService.Application.DTOs.Users;
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


    public class CreateUserCommanHandler : IRequestHandler<CreateUserCommand, Result<UserProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityServiceClient _identityServiceClient;
        private readonly ILogger<CreateUserCommanHandler> _logger;
        private readonly IMapper _mapper;


        public CreateUserCommanHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            IIdentityServiceClient identityServiceClient, 
            ILogger<CreateUserCommanHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            var dto=_mapper.Map<UserProfileDto>(userProfile);
            return Result<UserProfileDto>.Success(dto, "User created successfully");
        }
    }
}