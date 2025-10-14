using IdentityService.Application.Commands.Auth;
using IdentityService.Application.Commands.Users;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.DTOs.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.API.Controllers
{

    [ApiController]
    [Route("api/controller")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMediator mediator,
            ILogger<AuthController> logger)
        {
            _mediator = mediator;
            _logger=logger;
        }


        /// <summary>
        /// User login endpoint
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            // Get the client's IP address for security logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await _mediator.Send(new LoginCommand(request.username, request.password, ipAddress));

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for username: {Username} from IP: {IpAddress}",
                    request.username, ipAddress);
                return Unauthorized(result);
            }

            _logger.LogWarning("Login failed for username: {Username} from IP: {IpAddress}",
                request.username, ipAddress);

            return Ok(result);
        }



        ///<summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken, ipAddress));

            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }



        /// <summary>
        /// Logout user (revoke all refresh tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim,out var userId))
            {
                return Unauthorized(Result<bool>.Failure("Invalid token"));
            }

            var result = await _mediator.Send(new LogoutCommand(userId));

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {UserId} logged out succesfully", userId);
            return Ok(result);
        }




        /// <summary>
        /// Register new user (internal endpoint - will be called by User Management Service)
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous] // In production, this should be protected or internal-only
        [ProducesResponseType(typeof(UserDto),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(
            [FromBody] CreateUserRequestDto request,
            CancellationToken cancellationToken)
        {

            var result = await _mediator.Send(new CreateUserCommand(request.Username,request.Email,request.Password));

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {Username} registered succesfully", request.Username);
            return CreatedAtAction(nameof(Register), new { id = result.Data?.Id }, result);
        }
    }
}