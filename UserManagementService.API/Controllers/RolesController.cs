using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementService.Application.Commands.Roles;
using UserManagementService.Application.DTOs.Roles;

namespace UserManagementService.API.Controllers
{
    /// <summary>
    /// Controller for managing user roles.
    /// Only admins can assign or remove roles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class RolesController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IMediator mediator, ILogger<RolesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        /// <summary>
        /// Assign a role to a user.
        /// Only admins can perform this operation.
        /// </summary>
        [HttpPost("users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AssignRole(
            Guid userId,
            [FromBody] AssignRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            var assignedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(assignedByIdClaim) || !Guid.TryParse(assignedByIdClaim,out var assignedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new AssignRoleCommand(
                UserProfileId: userId,
                Role: request.Role,
                AssignedBy: assignedById,
                Reason: request.Reason);


            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Role {Role} assigned to user {UserId} by {AssignedBy}",
                request.Role,
                userId,
                assignedById);

            return Ok(result);
        }



        /// <summary>
        /// Remove a role from a user.
        /// Only admins can perform this operation.
        /// </summary>
        [HttpDelete("users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRole(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var removedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(removedByIdClaim) || !Guid.TryParse(removedByIdClaim, out var removedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new RemoveRoleCommand(userId, removedById);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation("Role removed from user {UserId} by {RemovedBy}", userId, removedById);
            return Ok(result);
        }
    }
}
