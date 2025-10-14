using FluentValidation;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Application.Commands.Auth
{
    public record LoginCommand(
        string Username,
        string Password,
        string IpAddress): IRequest<Result<LoginResponseDto>>;

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public LoginCommandHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            IConfiguration configuration)
        {
            _unitOfWork=unitOfWork;
            _tokenService=tokenService;
            _passwordHasher=passwordHasher;
            _configuration=configuration;
        }

        public async Task<Result<LoginResponseDto>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            // Find the user by username
            var user = await _unitOfWork.Users.GetFirstOrDefaultAsync(
                u => u.Username == request.Username,
                cancellationToken);

            // Check if user is not found or password is incorrect
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Result<LoginResponseDto>.Failure("Invalid username or password");
            }

            // Check if the account is active
            if (!user.IsActive)
            {
                return Result<LoginResponseDto>.Failure("User account is deactivated");
            }

            // Generate JWT access token
            var accessToken = _tokenService.GenerateAccessToken(user);

            // Generate Refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Get token expiration settings
            var refreshTokenExpirationDays = int.Parse(
                _configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

            var accessTokenExpirationMinutes = int.Parse(
                _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");


            // Create refresh tokens entity and save to database
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                CreatedAt=DateTime.UtcNow,
                CreatedByIp=request.IpAddress
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

            // Update user's last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            // Commit all changes to database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build and return the response DTO
            var response = new LoginResponseDto(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                UserId: user.Id,
                UserName: user.Username,
                Email: user.Email,
                ExpiresAt: DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes));

            return Result<LoginResponseDto>.Success(response, "Login succesfull");
        }
    }
}