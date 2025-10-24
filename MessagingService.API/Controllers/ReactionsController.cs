using MediatR;
using MessagingService.Application.Commands.Reactions;
using MessagingService.Application.Reactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingService.API.Controllers
{
    [ApiController]
    [Route("api/channels/{channelId:guid}/messages/{messageId:guid}/reactions")]
    [Authorize]
    [Produces("application/json")]
    public class ReactionsController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ReactionsController> _logger;
        public ReactionsController(
            IMediator mediator,
            ILogger<ReactionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }




        /// <summary>
        /// Add a reaction to a message.
        /// </summary>
        /// <param name="channelId">The channel containing the message</param>
        /// <param name="messageId">The message to react to</param>
        /// <param name="request">The reaction (emoji)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddReaction(
            Guid channelId,
            Guid messageId,
            [FromBody] ReactionRequestDto request,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new AddReactionCommand(
                MessageId: messageId,
                UserId: userId,
                Emoji: request.Emoji);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Reaction {Emoji} added to message {MessageId} by user {UserId}",
                request.Emoji,
                messageId,
                userId);

            return Ok(result);
        }





        /// <summary>
        /// Remove a reaction from a message.
        /// </summary>
        /// <param name="channelId">The channel containing the message</param>
        /// <param name="messageId">The message to remove reaction from</param>
        /// <param name="emoji">The emoji to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpDelete("{emoji}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveReaction(
            Guid channelId,
            Guid messageId,
            [FromBody] ReactionRequestDto request,
            CancellationToken cancellationToken)
        {
            var userIdClaim=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim,out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new RemoveReactionCommand(
                MessageId: messageId,
                UserId: userId,
                Emoji: request.Emoji);

            var result=await _mediator.Send(command,cancellationToken);

            if(!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Reaction {Emoji} removed from message {MessageId} by user {UserId}",
                request.Emoji,
                messageId,
                userId);

            return Ok(result);
        }
    }
}