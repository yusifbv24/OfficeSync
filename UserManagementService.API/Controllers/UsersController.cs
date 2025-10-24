using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementService.Application.Commands.Users;
using UserManagementService.Application.DTOs.Users;
using UserManagementService.Application.Queries.Users;
using UserManagementService.Domain.Entities;

namespace UserManagementService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IMediator mediator, ILogger<UsersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        [HttpPost]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            // Get the ID of the user making the request (from JWT token)
            var createdByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(createdByIdClaim) || !Guid.TryParse(createdByIdClaim, out var createdById))
            {
                return Unauthorized(new {Message="Invalid token"});
            }

            var command = new CreateUserCommand(
                Username: request.Username,
                Email: request.Email,
                Password: request.Password,
                DisplayName: request.DisplayName,
                AvatarUrl: request.AvatarUrl,
                Notes: request.Notes,
                CreatedBy: createdById);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "User created successfully: {UserId}, DisplayName: {DisplayName}",
                request.DisplayName,
                createdById);

            return CreatedAtAction(
                nameof(GetUserProfile),
                new { id = result.Data!.Id },
                result);
        }




        /// <summary>
        /// Get a user profile by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserProfile(Guid id, CancellationToken cancellationToken)
        {
            var query=new GetUserProfileByIdQuery(id);
            var result= await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Get a user profile by the UserId from Identity Service.
        /// This is used by other microservices to get user information
        /// based on the ID from the JWT token.
        /// </summary>
        [HttpGet("by-userid/{userId:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserProfileByUserId(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var query = new GetUserProfileByUserIdQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Get all users with pagination.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(UserListDto),StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAllUsersQuery(pageNumber, pageSize);
            var result = await _mediator.Send(query, cancellationToken);

            if(!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }




        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(
            Guid id,
            [FromBody] UpdateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserCommand(
                UserProfileId: id,
                DisplayName: request.DisplayName,
                AvatarUrl: request.AvatarUrl,
                Notes:request.Notes);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User profile {Id} updated successfully", id);
            return Ok(result);
        }



        /// <summary>
        /// Deactivate a user account.
        /// Only admins can deactivate users.
        /// </summary>
        [HttpPut("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
        {
            var deactivatedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(deactivatedByIdClaim) || !Guid.TryParse(deactivatedByIdClaim, out var deactivatedById))
            {
                return Unauthorized(new {Message="Invalid token"});
            }
            var command=new DeactivateUserCommand(id,deactivatedById);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {Id} deactivated by {DeactivatedBy}", id, deactivatedById);
            return Ok(result);
        }




        /// <summary>
        /// Delete a user account (soft delete).
        /// Only admins can delete users.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var deletedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(deletedByIdClaim) || !Guid.TryParse(deletedByIdClaim, out var deletedById))
            {
                return Unauthorized(new {Message="Invalid token"});
            }

            var command=new DeleteUserCommand(id,deletedById);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("User {Id} deleted by {DeletedBy}", id, deletedById);
            return Ok(result);
        }
    }
}