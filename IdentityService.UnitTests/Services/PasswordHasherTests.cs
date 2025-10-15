using FluentAssertions;
using IdentityService.Infrastructure.Services;

namespace IdentityService.UnitTests.Services
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void HashPassword_ShouldReturnHashedPassword()
        {
            // Arrange
            var password = "SecurePassword123!";

            // Act
            var hash = _passwordHasher.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password);
            hash.Should().StartWith("$2");
        }



        [Fact]
        public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
        {
            // Arrange
            var password = "SamePassword";
            // Act
            var hash1 = _passwordHasher.HashPassword(password);
            var hash2 = _passwordHasher.HashPassword(password);
            // Assert
            hash1.Should().NotBe(hash2);
        }



        [Fact]
        public void HashPassword_ShouldThrowExceptionForEmptyPassword()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword(string.Empty));
        }



        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            var password = "CorrectPassword!";
            var hash = _passwordHasher.HashPassword(password);
            // Act
            var result = _passwordHasher.VerifyPassword(password, hash);
            // Assert
            result.Should().BeTrue();
        }


        [Fact]
        public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
        {
            // Arrange
            var password = "CorrectPassword!";
            var wrongPassword = "WrongPassword!";
            var hash = _passwordHasher.HashPassword(password);
            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hash);
            // Assert
            result.Should().BeFalse();
        }



        [Fact]
        public void VerifyPassword_ShouldReturnFalseForEmptyPassword()
        {
            // Arrange
            var password = "SomePassword!";
            var hash = _passwordHasher.HashPassword(password);
            // Act
            var result = _passwordHasher.VerifyPassword(string.Empty, hash);
            // Assert
            result.Should().BeFalse();
        }



        [Fact]
        public void VerifyPassword_ShouldReturnFalseForEmptyHash()
        {
            // Arrange
            var password = "SomePassword!";
            var hash = _passwordHasher.HashPassword(password);

            // Act

            var result = _passwordHasher.VerifyPassword(password, string.Empty);

            // Assert
            result.Should().BeFalse();
        }


        [Fact]
        public void VerifyPassword_ShouldReturnFalseForMalformedHash()
        {
            // Arrange
            var malformedHash = "not-a-valid-hash";

            // Act
            var result = _passwordHasher.VerifyPassword("TestPassword123!", malformedHash);

            // Assert
            result.Should().BeFalse();
        }
    }
}