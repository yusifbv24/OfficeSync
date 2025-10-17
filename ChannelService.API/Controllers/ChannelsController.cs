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




    }
}