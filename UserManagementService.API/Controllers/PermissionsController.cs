using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementService.Application.Commands.Permissions;
using UserManagementService.Application.DTOs.Permissions;

namespace UserManagementService.API.Controllers
{
    /// <summary>
    /// Controller for managing user-specific permissions.
    /// Admins can grant any permissions.
    /// Operators can grant limited permissions within their scope.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PermissionsController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(IMediator mediator,ILogger<PermissionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }



        /// <summary>
        /// Grant specific permissions to a user.
        /// </summary>
        [HttpPost("users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GrantPermission(
            Guid userId,
            [FromBody] GrantPermissionsRequestDto request,
            CancellationToken cancellationToken)
        {
            var grantedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(grantedByIdClaim) || !Guid.TryParse(grantedByIdClaim,out var grantedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new GrantPermissionsCommand(
                UserProfileId: userId,
                GrantedBy: grantedById,
                CanManageUsers: request.CanManageUsers,
                CanManageChannels: request.CanManageChannels,
                CanDeleteMessages: request.CanDeleteMessages,
                CanManageRoles: request.CanManageRoles,
                SpecificChannelIds: request.SpecificChannelIds,
                ExpiresAt: request.ExpiresAt);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            _logger?.LogInformation(
                   "Permissions granted to user {UserId} by {GrantedBy}",
                   userId,
                   grantedById);

            return Ok(result);
        }




        /// <summary>
        /// Revoke all specific permissions from a user.
        /// </summary>
        [HttpDelete("users/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokePermissions(
            Guid userId,
            CancellationToken cancellationToken)

        {
            var revokedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(revokedByIdClaim) || !Guid.TryParse(revokedByIdClaim, out var revokedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new RevokePermissionsCommand(userId, revokedById);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Permissions revoked from user {UserId} by {RevokedBy}", userId, revokedById);
            return Ok(result);
        }
    }
}