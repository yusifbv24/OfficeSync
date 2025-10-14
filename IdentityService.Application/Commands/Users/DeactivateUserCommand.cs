using IdentityService.Application.Common;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Commands.Users
{
    public record DeactivateUserCommand(
        Guid userId):IRequest<Result<bool>>;

    public class DeactivateUserCommandHandler: IRequestHandler<DeactivateUserCommand , Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public DeactivateUserCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.userId, cancellationToken);

            if (user == null)
            {
                return Result<bool>.Failure("User not found");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true, "User deactivated succesfully");
        }
    }
}