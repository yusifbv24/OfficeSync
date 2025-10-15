using FluentValidation.TestHelper;
using IdentityService.Application.Commands.Users;

namespace IdentityService.UnitTests.Validators
{
    public class CreateUserCommandValidatorTests
    {
        private readonly CreateUserCommandValidator _validator;
        public CreateUserCommandValidatorTests()
        {
            _validator=new CreateUserCommandValidator();
        }

        [Fact]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }


        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WithEmptyUsername_ShouldHaveError(string username)
        {
            // Arrange
            var command=new CreateUserCommand(
                username,
                "valid@example.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username);
        }



        [Theory]
        [InlineData("ab")]
        [InlineData("a")]
        public void Validate_WithShortUsername_ShouldHaveError(string username)
        {
            // Arrange
            var command = new CreateUserCommand(
                username,
                "valid@example.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username must be at least 3 characters");
        }



        [Fact]
        public void Validate_WithLongUsername_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                new string('a', 31),
                "valid@example.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert

            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username must not exceed 30 characters");
        }



        [Theory]
        [InlineData("user name")] // Space
        [InlineData("user@name")] // @ symbol
        [InlineData("user#name")] // # symbol
        public void Validate_WithInvalidUsernameCharacters_ShouldHaveError(string username)
        {
            // Arrange
            var command = new CreateUserCommand(
                username,
                "valid@example.com",
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username)
                .WithErrorMessage("Username can only contain letters,numbers,underscores and hyphens");
        }




        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WithEmptyEmail_ShouldHaveError(string email)
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                email!,
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }



        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public void Validate_WithInvalidEmailFormat_ShouldHaveError(string email)
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                email,
                "Password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format");
        }



        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WithEmptyPassword_ShouldHaveError(string password)
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                password!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }



        [Fact]
        public void Validate_WithShortPassword_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "Pass1!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must be at least 8 characters");
        }



        [Fact]
        public void Validate_WithoutUppercase_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "password123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one uppercase letter");
        }



        [Fact]
        public void Validate_WithoutLowercase_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "PASSWORD123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one lowercase letter");
        }



        [Fact]
        public void Validate_WithoutNumber_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "Password!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one number");
        }




        [Fact]
        public void Validate_WithoutSpecialCharacter_ShouldHaveError()
        {
            // Arrange
            var command = new CreateUserCommand(
                "validuser",
                "valid@example.com",
                "Password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one special character");
        }
    }
}