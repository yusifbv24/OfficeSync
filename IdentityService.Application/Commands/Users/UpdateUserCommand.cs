using AutoMapper;
using FluentValidation;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.User;
using IdentityService.Application.Interfaces;
using MediatR;

namespace IdentityService.Application.Commands.Users
{
    public record UpdateUserCommand(
    Guid UserId,
    string? Username,
    string? Email) : IRequest<Result<UserDto>>;

    public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(30).WithMessage("Username must not exceed 30 characters")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters,numbers,underscores and hyphens");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }


    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpdateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork= unitOfWork;
            _mapper= mapper;
        }

        public async Task<Result<UserDto>> Handle(UpdateUserCommand request,CancellationToken cancellationToken)
        {
            // Find the user
            var user=await _unitOfWork.Users.GetFirstOrDefaultAsync(u=>u.Id==request.UserId,cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            // Update username if provided
            if(!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
            {
                // Check if new username is already taken
                var usernameExists = await _unitOfWork.Users.ExistsAsync(
                    u => u.Username == request.Username,
                    cancellationToken);

                if(usernameExists)
                {
                    return Result<UserDto>.Failure("New Username already taken");
                }

                user.Username=request.Username;
            }

            // Update email if provided
            if(!string.IsNullOrEmpty(request.Email) &&  request.Email != user.Email)
            {
                // Check if new email is already taken
                var emailExists = await _unitOfWork.Users.ExistsAsync(
                    u => u.Email == request.Email,
                    cancellationToken);

                if(emailExists)
                {
                    return Result<UserDto>.Failure("New Email already taken");
                }

                user.Email=request.Email;
            }

            // Update timestamp
            user.UpdatedAt=DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var userDto= _mapper.Map<UserDto>(user);

            return Result<UserDto>.Success(userDto, "User updated successfully");
        }
    }
}