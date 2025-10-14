using FluentAssertions;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdentityService.UnitTests.Services
{
    public class TokenServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly TokenService _tokenService;
        private readonly string _testSecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456";

        public TokenServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock.Setup(x => x["Jwt:SecretKey"]).Returns(_testSecretKey);
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
            _configurationMock.Setup(x => x["Jwt:AccessTokenExpirationMinutes"]).Returns("60");

            _tokenService = new TokenService(_configurationMock.Object);
        }

        [Fact]
        public void GenerateAccessToken_ShouldReturnValidJwtToken()
        {
            // Arrange

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                IsActive = true
            };

            // Act 
            var token=_tokenService.GenerateAccessToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken=handler.ReadJwtToken(token);

            jwtToken.Should().NotBeNull();
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        }


        [Fact]
        public void GenerateRefreshToken_ShouldReturnBase64String()
        {
            // Act
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrEmpty(); 
            refreshToken.Length.Should().BeGreaterThan(40); // Base64 of 64 bytes should be longer

            // Verify it's valid Base64
            var bytes = Convert.FromBase64String(refreshToken);
            bytes.Should().NotBeNull();
            bytes.Length.Should().Be(64);
        }



        [Fact]
        public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
        {
            // Act
            var token1=_tokenService.GenerateRefreshToken();
            var token2=_tokenService.GenerateRefreshToken();

            // Assert
            token1.Should().NotBe(token2);
        }



        [Fact]
        public void ValidateAccessToken_WithValidToken_ShouldReturnUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };

            var token=_tokenService.GenerateAccessToken(user);

            // Act
            var result=_tokenService.ValidateAccessToken(token);

            // Assert
            result.Should().Be(userId);
        }



        [Fact]
        public void ValidateAccessToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token";

            // Act
            var result= _tokenService.ValidateAccessToken(invalidToken);

            // Assert
            result.Should().BeNull();
        }



        [Fact]
        public void ValidateAccessToken_WithEmptyToken_ShouldReturnNull()
        {
            // Act
            var result = _tokenService.ValidateAccessToken("");

            // Assert
            result.Should().BeNull();
        }

    }
}