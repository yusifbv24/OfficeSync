using ChannelService.Application.Commands.Members;
using ChannelService.Application.Members;
using ChannelService.Application.Queries.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChannelService.API.Controllers
{
    [ApiController]
    [Route("api/channels/{channelId:guid}/members")]
    [Authorize]
    [Produces("application/json")]
    public class MembersController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MembersController> _logger;
        public MembersController(IMediator mediator, ILogger<MembersController> logger)
        {
            _mediator=mediator;
            _logger=logger;
        }



        /// <summary>
        /// Get all members of a channel.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ChannelMemberDto>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMembers(
            Guid channelId,
            CancellationToken cancellationToken)
        {
            var requestedByIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(requestedByIdClaim) || !Guid.TryParse(requestedByIdClaim, out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query = new GetChannelMembersQuery(
                ChannelId: channelId,
                RequestedBy: requestedById);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }



        /// <summary>
        /// Add a member to a channel.
        /// Only owners and moderators can add members.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddMember(
            Guid channelId,
            [FromBody] AddMemberRequestDto request,
            CancellationToken cancellationToken)
        {
            var addedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(addedByIdClaim) || !Guid.TryParse(addedByIdClaim, out var addedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }
            var command = new AddMemberCommand(
                ChannelId: channelId,
                UserId: request.UserId,
                AddedBy: addedById,
                Role: request.Role);

            var result=await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            _logger?.LogInformation(
                "Member {UserId} added to channel {ChannelId} by {AddedBy}",
                request.UserId,
                channelId,
                addedById);

            return Ok(result);
        }




        /// <summary>
        /// Remove a member from a channel.
        /// Owners can remove anyone. Moderators can remove members only.
        /// </summary>
        [HttpDelete("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveMember(
            Guid channelId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var removedByClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(removedByClaim) || !Guid.TryParse(removedByClaim, out var removedBy))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }
            var command = new RemoveMemberCommand(
                ChannelId: channelId,
                UserId: userId,
                RemovedBy: removedBy);

            var result = await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
               "Member {UserId} removed from channel {ChannelId} by {RemovedBy}",
               userId,
               channelId,
               removedBy);

            return Ok(result);
        }



        /// <summary>
        /// Change a member's role in the channel.
        /// Only owners can change roles.
        /// </summary>
        [HttpPut("{userId:guid}/role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeMemberRole(
            Guid channelId,
            Guid userId,
            [FromBody] ChangeMemberRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            var changedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(changedByIdClaim) || ! Guid.TryParse(changedByIdClaim, out var changedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new ChangeMemberRoleCommand(
                ChannelId: channelId,
                UserId: userId,
                Role: request.Role,
                ChangedBy: changedById);

            var result= await _mediator.Send(command,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                   "Member {UserId} role changed to {Role} in channel {ChannelId} by {ChangedBy}",
                   userId,
                   request.Role,
                   channelId,
                   changedById);

            return Ok(result);
        }
    }
}