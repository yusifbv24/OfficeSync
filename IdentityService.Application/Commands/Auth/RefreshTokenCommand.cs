using IdentityService.Application.Common;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Application.Commands.Auth
{
    public record RefreshTokenCommand(
        string RefreshToken,
        string IpAddress) : IRequest<Result<LoginResponseDto>>;

    public class RefreshTokenCommandHandler: IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuraion;

        public RefreshTokenCommandHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _unitOfWork= unitOfWork;
            _tokenService= tokenService;
            _configuraion= configuration;
        }

        public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request,CancellationToken cancellationToken)
        {
            // Find the refresh token in the database
            var token = await _unitOfWork.RefreshTokens.GetFirstOrDefaultAsync(
                rt => rt.Token == request.RefreshToken,
                cancellationToken);

            if (token == null)
            {
                return Result<LoginResponseDto>.Failure("Invalid refresh token");
            }

            // Check if token is still active (not expired or revoked)
            if(!token.IsActive)
            {
                return Result<LoginResponseDto>.Failure("Refresh token is no longer valid");
            }

            // Get the associated user
            var user = await _unitOfWork.Users.GetByIdAsync(token.UserId, cancellationToken);

            if(user==null || !user.IsActive)
            {
                return Result<LoginResponseDto>.Failure("User not found or deactivated");
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();


            // Get expiration settings
            var refreshTokenExpirationDays = int.Parse(
                _configuraion["Jwt:RefreshTokenExpirationDays"] ?? "7");
            var accessTokenExpirationMinutes = int.Parse(
                _configuraion["Jwt:AccessTokenExpirationMinutes"] ?? "60");

            // Revoke old refresh token
            token.RevokedAt = DateTime.Now;
            token.RevokedByIp = request.IpAddress;
            token.ReplacedByToken = newRefreshToken;
            await _unitOfWork.RefreshTokens.UpdateAsync(token, cancellationToken);

            // Create new refresh token entity
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.Now.AddDays(refreshTokenExpirationDays),
                CreatedAt= DateTime.Now,
                CreatedByIp= request.IpAddress
            };

            await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return new tokens
            var response = new LoginResponseDto(
                AccessToken: newAccessToken,
                RefreshToken: newRefreshToken,
                UserId: user.Id,
                UserName: user.Username,
                Email: user.Email,
                ExpiresAt: DateTime.Now.AddMinutes(accessTokenExpirationMinutes)
            );

            return Result<LoginResponseDto>.Success(response, "Token refreshed succesfully");
        }
    }
}