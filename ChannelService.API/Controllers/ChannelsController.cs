using ChannelService.Application.Channels;
using ChannelService.Application.Commands.Channels;
using ChannelService.Application.Queries.Channels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChannelService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ChannelsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChannelsController> _logger;

        public ChannelsController(IMediator mediator, ILogger<ChannelsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }



        [HttpPost]
        [ProducesResponseType(typeof(ChannelDto),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateChannel(
            [FromBody] CreateChannelRequestDto request,
            CancellationToken cancellationToken)
        {
            var createdByIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(createdByIdClaim) || !Guid.TryParse(createdByIdClaim, out Guid createdById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }


            var command =new CreateChannelCommand(
                Name: request.Name,
                Description: request.Description,
                Type: request.Type,
                CreatedBy: createdById);

            var result = await _mediator.Send(command,cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Channel created: {ChannelId}, Name: {Name}, by {CreatedBy}",
                result.Data!.Id,
                result.Data.Name,
                createdById);

            return CreatedAtAction(
                nameof(GetChannel),
                new { id = result.Data!.Id },
                result);
        }




        /// <summary>
        /// Get a channel by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ChannelDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetChannel(
            Guid id, 
            CancellationToken cancellationToken)
        {
            var requetedByIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(requetedByIdClaim) || !Guid.TryParse(requetedByIdClaim,out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query=new GetChannelByIdQuery(id,requestedById);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }





        /// <summary>
        /// Get all channels with pagination.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ChannelListDto),StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllChannels(
            [FromQuery] int pageNumber=1,
            [FromQuery] int pageSize=20,
            [FromQuery] bool includeArchived=false,
            CancellationToken cancellationToken = default)
        {
            var requestedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(requestedByIdClaim) || !Guid.TryParse(requestedByIdClaim,out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid Token" });
            }

            var query = new GetAllChannelsQuery(
                RequestedBy: requestedById,
                PageNumber: pageNumber,
                PageSize: pageSize,
                IncludeArchived: includeArchived);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }



        /// <summary>
        /// Get channels where the user is a member.
        /// </summary>
        [HttpGet("my-channels")]
        [ProducesResponseType(typeof(ChannelListDto),StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyChannels(
            [FromQuery] int pageNumber=1,
            [FromQuery] int pageSize=20,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim,out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query = new GetUserChannelsQuery(
                UserId: userId,
                PageNumber: pageNumber,
                PageSize: pageSize);

            var result=await _mediator.Send(query,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }




        /// <summary>
        /// Update channel information
        /// Only channel owners can update
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ChannelDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateChannel(
            Guid id,
            [FromBody] UpdateChannelRequestDto request,
            CancellationToken cancellationToken)
        {
            var updatedByIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(updatedByIdClaim) || !Guid.TryParse(updatedByIdClaim, out var updatedById))
            {
                return BadRequest(new { Message = "Invalid token" });
            }

            var command = new UpdateChannelCommand(
                ChannelId: id,
                Name: request.Name,
                Description: request.Description,
                UpdatedBy: updatedById);

            var result= await _mediator.Send(command,cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            _logger.LogInformation("Channel {ChannelId} updated by {UpdatedBy}", id, updatedById);
            return Ok(result);
        }



        /// <summary>
        /// Archive a channel.
        /// Only channel owners can archive.
        /// </summary>
        [HttpPut("{id:guid}/archive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ArchiveChannel(
            Guid id,
            CancellationToken cancellationToken)
        {
            var archivedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(archivedByIdClaim) || !Guid.TryParse(archivedByIdClaim, out var archivedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new ArchiveChannelCommand(
                ChannelId: id,
                ArchivedBy: archivedById);

            var result=await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Channel {ChannelId} archived by {ArchivedBy}", id, archivedById);

            return Ok(result);
        }



        /// <summary>
        /// Unarchive a channel.
        /// Only channel owners can unarchive.
        /// </summary>
        [HttpPut("{id:guid}/unarchive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UnarchiveChannel(
            Guid id,
            CancellationToken cancellationToken)
        {
            var unarchivedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(unarchivedByIdClaim) || !Guid.TryParse(unarchivedByIdClaim,out var unarchivedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new UnarchiveChannelCommand(id, unarchivedById);
            var result = await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            _logger.LogInformation("Channel {ChannelId} unarchived by {UnarchivedBy}", id, unarchivedById);
            return Ok(result);
        }
    }
}