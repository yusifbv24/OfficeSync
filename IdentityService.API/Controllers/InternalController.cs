using IdentityService.Application.Commands.Users;
using IdentityService.Application.DTOs.User;
using IdentityService.Application.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.API.Controllers
{
    /// <summary>
    /// Internal endpoints for inter-service communication
    /// These endpoints are called by other microservices, not by end users.
    /// In production, secure these with service-to-service authentication.
    /// </summary>
    [ApiController]
    [Route("api/internal")]
    [Authorize] // In production, use service tokens instead of user tokens
    [Produces("application/json")]
    public class InternalController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InternalController> _logger;
        public InternalController(IMediator mediator,ILogger<InternalController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }



        /// <summary>
        /// Get user by ID (for other services)
        /// </summary>
        [HttpGet("users/{userId:guid}")]
        [ProducesResponseType(typeof(UserDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUser(Guid userId,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(userId),cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }




        ///<summary>
        /// Get user by username (for other services)
        /// </summary>
        [HttpGet("users/username/{username}")]
        [ProducesResponseType(typeof(UserDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByUsername(string username,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByUsernameQuery(username),cancellationToken);

            if(!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Update user information
        /// </summary>
        [HttpPut("users/{userId:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(
            Guid userId,
            [FromBody] UpdateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserCommand(userId, request.Username, request.Email);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {UserId} updated successfully", userId);
            return Ok(result);
        }





        /// <summary>
        /// Deactivate user (called by User Management Service)
        /// </summary>
        [HttpPut("users/{userId:guid}/deactivate")]
        [ProducesResponseType(typeof(UserDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateUser(Guid userId,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeactivateUserCommand(userId), cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {UserId} deactivated successfully", userId);
            return Ok(result);
        }





        /// <summary>
        /// Activate user (called by User Management Service)
        /// </summary>
        [HttpPut("users/{userId:guid}/activate")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateUser(Guid userId,CancellationToken cancellationToken=default)
        {
            var result = await _mediator.Send(new ActivateUserCommand(userId), cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {UserId} activated successfully", userId);
            return Ok(result);
        }





        [HttpPut("users/{userId:guid}/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(
        Guid userId,
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return Ok(result);
        }
    }
}