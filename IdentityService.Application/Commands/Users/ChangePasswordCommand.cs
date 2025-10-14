using AutoMapper;
using FluentValidation;
using IdentityService.Application.Common;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Commands.Users
{
    public record ChangePasswordCommand(
        Guid userId,
        string CurrentPassword,
        string NewPassword): IRequest<Result<bool>>;

    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.NewPassword)
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from current password");
        }
    }


    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPasswordHasher passwordHasher)
        {
            _unitOfWork=unitOfWork;
            _mapper=mapper;
            _passwordHasher=passwordHasher;
        }

        public async Task<Result<bool>> Handle(ChangePasswordCommand request,CancellationToken cancellationToken)
        {
            // Find the user
            var user = await _unitOfWork.Users.GetByIdAsync(request.userId, cancellationToken);
            if (user == null)
            {
                return Result<bool>.Failure("User not found");
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result<bool>.Failure("Current password is incorrect");
            }

            // Hash and set the new password
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true, "Password changed succesfully");
        }
    }
}