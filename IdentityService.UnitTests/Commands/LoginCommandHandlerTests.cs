using FluentAssertions;
using IdentityService.Application.Commands.Auth;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq.Expressions;

namespace IdentityService.UnitTests.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepositoryMock;
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _unitOfWorkMock=new Mock<IUnitOfWork>();
            _tokenServiceMock=new Mock<ITokenService>();
            _passwordHasherMock=new Mock<IPasswordHasher>();
            _configurationMock=new Mock<IConfiguration>();
            _userRepositoryMock=new Mock<IRepository<User>>();
            _refreshTokenRepositoryMock=new Mock<IRepository<RefreshToken>>();

            // Setup
            _configurationMock.Setup(x => x["Jwt:RefreshTokenExpirationDays"]).Returns("7");
            _configurationMock.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("60");

            // Setup unit of work
            _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);

            _handler = new LoginCommandHandler(
                _unitOfWorkMock.Object,
                _tokenServiceMock.Object,
                _passwordHasherMock.Object,
                _configurationMock.Object);
        }


        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash="hashedpassword",
                IsActive = true
            };

            var command = new LoginCommand("testuser", "password123", "127.0.0.1");
            _userRepositoryMock
                .Setup(x => x.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword("password123", "hashedpassword"))
                .Returns(true);

            _tokenServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access-token");

            _tokenServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh-token");


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().Be("access-token");
            result.Data.RefreshToken.Should().Be("refresh-token");
            result.Data.UserId.Should().Be(userId);
            result.Data.UserName.Should().Be("testuser");
            result.Data.Email.Should().Be("test@example.com");

            _refreshTokenRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }


        [Fact]
        public async Task Handle_WithInvalidUsername_ShouldReturnFailureResult()
        {
            // Arrange
            var command = new LoginCommand("nonexistent", "password123", "127.0.0.1");

            _userRepositoryMock
                .Setup(x => x.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid username or password");
        }


        [Fact]
        public async Task Handle_WithInvalidPassword_ShouldReturnFailureResult()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                IsActive = true
            };

            var command = new LoginCommand("testuser", "wrongpasswrd", "127.0.0.1");
            _userRepositoryMock
                .Setup(x => x.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword("wrongpassword", "hashedpassword"))
                .Returns(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);


            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid username or password");
        }


        [Fact]
        public async Task Handle_WithInActiveUser_ShouldReturnFailureResult()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                IsActive = false
            };

            var command = new LoginCommand("testuser", "password123", "127.0.0.1");

            _userRepositoryMock
                .Setup(x => x.GetFirstOrDefaultAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.VerifyPassword("password123", "hashedpassword"))
                .Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);


            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("User account is deactivated");
        }
    }
}