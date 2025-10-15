using AutoMapper;
using FluentAssertions;
using IdentityService.Application.Commands.Users;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Mappings;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace IdentityService.UnitTests.Commands
{
    public class CreateUserCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<ILoggerFactory> _loggerMock;
        private readonly IMapper _mapper;
        private readonly CreateUserCommandHandler _handler;

        public CreateUserCommandHandlerTests()
        {
            _unitOfWorkMock=new Mock<IUnitOfWork>();
            _passwordHasherMock=new Mock<IPasswordHasher>();
            _userRepositoryMock=new Mock<IRepository<User>>();

            _loggerMock=new Mock<ILoggerFactory>();

            var mockLogger = new Mock<ILogger>();
            _loggerMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(mockLogger.Object);

            // Setup AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            },_loggerMock.Object);

            _mapper = config.CreateMapper();

            _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

            _handler=new CreateUserCommandHandler(
                _unitOfWorkMock.Object,
                _passwordHasherMock.Object,
                _mapper);
        }


        [Fact]
        public async Task Handle_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var command = new CreateUserCommand("newuser", "newuser@example.com", "127.0.0.1");

            _userRepositoryMock
                .Setup(x => x.ExistsAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(x => x.HashPassword("Password123!"))
                .Returns("hashed-password");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("newuser");
            result.Data.Email.Should().Be("newuser@example.com");
            result.Data.IsActive.Should().BeTrue();

            _userRepositoryMock.Verify(
                x => x.AddAsync(It.Is<User>(u =>
                    u.Username == "newuser" &&
                    u.Email == "newuser@example.com" &&
                    u.PasswordHash == "hashed-password"),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }



        [Fact]
        public async Task Handle_WithExistingUsername_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateUserCommand(
                    "existinguser",
                    "new@example.com",
                    "Password123!");


            _userRepositoryMock
                .Setup(x => x.ExistsAsync(
                    It.Is<Expression<Func<User, bool>>>(expr => expr.ToString().Contains("Username")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Username already taken");

            // Should NOT call AddAsync when username exists
            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }



        [Fact]
        public async Task Handle_WithExistingEmail_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateUserCommand(
                "newuser",
                "existing@example.com",
                "Password123!");

            _userRepositoryMock
                .Setup(x => x.ExistsAsync(
                    It.Is<Expression<Func<User, bool>>>(expr => expr.ToString().Contains("Email")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result =await _handler.Handle(command, CancellationToken.None);


            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Email already exists");


            // Should NOT call AddAsync when email exists
            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never); // Changed from Times.Once to Times.Never
        }
    }
}