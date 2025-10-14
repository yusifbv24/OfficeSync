using AutoMapper;
using FluentValidation;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.User;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using MediatR;

namespace IdentityService.Application.Commands.Users
{
    public record CreateUserCommand(
        string Username,
        string Email,
        string Password): IRequest<Result<UserDto>>;

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(30).WithMessage("Username must not exceed 30 characters")
                .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters,numbers,underscores and hyphens");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }

    public class CreateUserCommandHandler: IRequestHandler<CreateUserCommand, Result<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public CreateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(CreateUserCommand request,CancellationToken cancellationToken)
        {
            // Check if username already exists
            var usernameExists = await _unitOfWork.Users.ExistsAsync(u => u.Username == request.Username, cancellationToken);

            if (usernameExists)
            {
                return Result<UserDto>.Failure("Username already taken");
            }

            // Check if email already exists
            var emailExists = await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return Result<UserDto>.Failure("Email already exists");
            }

            // Create the new user entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map entity to DTO
            var userDto=_mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto, "User created successfully");
        }
    }
}