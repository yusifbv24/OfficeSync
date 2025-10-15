using FluentAssertions;
using IdentityService.Application.DTOs.Auth;
using System.Net;
using System.Net.Http.Json;

namespace IdentityService.IntegrationTests.Controllers
{
    public class AuthControllerTests: IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }


        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var loginRequest=new LoginRequestDto("testuser", "TestPassword123!");

            // Act
            var response=await _client.PostAsJsonAsync("/api/controller/login", loginRequest,CancellationToken.None);
            var content=await response.Content.ReadFromJsonAsync< LoginResponseWrapper>(CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content!.IsSuccess.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.AccessToken.Should().NotBeNullOrEmpty();
            content.Data.RefreshToken.Should().NotBeNullOrEmpty();
            content.Data.UserName.Should().Be("testuser");
            content.Data.Email.Should().Be("testuser@example.com");
        }




        [Fact]
        public async Task Login_WithInvalidUsername_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest=new LoginRequestDto("invaliduser", "TestPassword123!");

            // Act
            var response=await _client.PostAsJsonAsync("/api/controller/login", loginRequest,CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }



        [Fact]
        public async Task Login_WithInactiveUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest=new LoginRequestDto("inactiveuser", "InactivePassword123!");

            // Act 
            var response=await _client.PostAsJsonAsync("/api/controller/login", loginRequest,CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }



        [Fact]
        public async Task Login_WithEmptyUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequestDto(string.Empty, "Somepassword!");

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/login", loginRequest, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var loginRequest=new LoginRequestDto("testuser", "TestPassword123!");
            var loginResponse=await _client.PostAsJsonAsync("/api/controller/login", loginRequest,CancellationToken.None);
            var loginContent=await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>(CancellationToken.None);

            var refreshRequest=new RefreshTokenRequestDto(loginContent!.Data!.RefreshToken);

            // Act
            var response=await _client.PostAsJsonAsync("/api/controller/refresh", refreshRequest,CancellationToken.None);
            var content=await response.Content.ReadFromJsonAsync<LoginResponseWrapper>(CancellationToken.None);


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content!.IsSuccess.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.AccessToken.Should().NotBeNullOrEmpty();
            content.Data.RefreshToken.Should().NotBeNullOrEmpty();
            content.Data.RefreshToken.Should().NotBe(loginContent.Data.RefreshToken);
        }



        [Fact]
        public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            var refreshRequest = new RefreshTokenRequestDto("invalid-refresh-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/refresh", refreshRequest,CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }



        [Fact]
        public async Task Register_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var registerRequest = new
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "NewPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/register", registerRequest,CancellationToken.None);
            var content = await response.Content.ReadFromJsonAsync<UserResponseWrapper>(CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            content.Should().NotBeNull();
            content!.IsSuccess.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.Username.Should().Be("newuser");
            content.Data.Email.Should().Be("newuser@example.com");
            content.Data.IsActive.Should().BeTrue();
        }



        [Fact]
        public async Task Register_WithExistingUsername_ShouldReturnBadRequest()
        {
            // Arrange
            var registerRequest = new
            {
                Username = "testuser", // This user already exists
                Email = "different@example.com",
                Password = "NewPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/register", registerRequest, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var registerRequest = new
            {
                Username = "differentuser",
                Email = "testuser@example.com", // This email already exists
                Password = "NewPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/register", registerRequest, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }




        [Fact]
        public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var registerRequest = new
            {
                Username = "weakpassuser",
                Email = "weakpass@example.com",
                Password = "weak" // Password doesn't meet requirements
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/controller/register", registerRequest, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }



        [Fact]
        public async Task Logout_WithValidToken_ShouldRevokeTokens()
        {
            // Arrange - First login
            var loginRequest = new LoginRequestDto("testuser", "TestPassword123!");
            var loginResponse = await _client.PostAsJsonAsync("/api/controller/login", loginRequest, CancellationToken.None);
            var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>(CancellationToken.None);

            // Add authorization header
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginContent!.Data!.AccessToken);

            // Act
            var response = await _client.PostAsync("/api/controller/logout", null, CancellationToken.None);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Try to use the refresh token - it should be revoked
            var refreshRequest = new RefreshTokenRequestDto(loginContent.Data.RefreshToken);
            var refreshResponse = await _client.PostAsJsonAsync("/api/controller/refresh", refreshRequest,CancellationToken.None);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Helper classes for deserialization
        private record LoginResponseWrapper
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public LoginResponseDto? Data { get; set; }
        }

        private record UserResponseWrapper
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public UserData? Data { get; set; }
        }

        private record UserData
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}