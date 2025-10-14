using IdentityService.Application.Common;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Commands.Auth
{
    public record LogoutCommand(
        Guid userId): IRequest<Result<bool>>;

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public LogoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<Result<bool>> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            // Find all active refresh tokens for this User
            var tokens = await _unitOfWork.RefreshTokens.FindAsync(
                rt => rt.UserId == request.userId,
                cancellationToken);

            // Revoke each token
            foreach(var token in tokens)
            {
                token.RevokedAt = DateTime.Now;
                token.RevokedByIp = "system";
                await _unitOfWork.RefreshTokens.UpdateAsync(token, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true, "Logged out succesfully");
        }
    }
}