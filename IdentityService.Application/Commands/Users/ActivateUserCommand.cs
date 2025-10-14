using IdentityService.Application.Common;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Commands.Users
{
    public record ActivateUserCommand(Guid userId):IRequest<Result<bool>>;

    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ActivateUserCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.userId, cancellationToken);

            if (user == null)
            {
                return Result<bool>.Failure("User not found");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true, "User activated succesfully");
        }
    }
}