using MediatR;
using MessagingService.Application.Commands.Messages;
using MessagingService.Application.Common;
using MessagingService.Application.Messages;
using MessagingService.Application.Queries.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingService.API.Controllers
{
    [ApiController]
    [Route("api/channels/{channelId:guid}/messages")]
    [Authorize]
    [Produces("application/json")]
    public class MessagesController:ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMediator mediator,
            ILogger<MessagesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }


        /// <summary>
        /// Send a new message to a channel.
        /// </summary>
        /// <param name="channelId">The channel to send the message to</param>
        /// <param name="request">Message content and metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created message</returns>
        [HttpPost]
        [ProducesResponseType(typeof(MessageDto),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendMessage(
            Guid channelId,
            [FromBody] SendMessageRequestDto request,
            CancellationToken cancellationToken)
        {
            var senderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(senderIdClaim) || !Guid.TryParse(senderIdClaim, out var senderId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            // Create command
            var command = new SendMessageCommand(
                ChannelId: channelId,
                SenderId: senderId,
                Content: request.Content,
                Type: request.Type,
                ParentMessageId: request.ParentMessageId);

            // Execute command
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Message {MessageId} sent to channel {ChannelId} by user {SenderId}",
                result.Data!.Id,
                channelId,
                senderId);

            return CreatedAtAction(
                nameof(GetMessage),
                new { channelId, messageId = result.Data!.Id },
                result);
        }





        /// <summary>
        /// Get a specific message by ID.
        /// </summary>
        /// <param name="channelId">The channel containing the message</param>
        /// <param name="messageId">The message ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The message details</returns>
        [HttpGet("{messageId:guid}")]
        [ProducesResponseType(typeof(MessageDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMessage(
            Guid channelId,
            Guid messageId,
            CancellationToken cancellationToken)
        {
            var requestedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(requestedByIdClaim) || !Guid.TryParse(requestedByIdClaim, out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query = new GetMessageByIdQuery(messageId, requestedById);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Get paginated messages from a channel.
        /// </summary>
        /// <param name="channelId">The channel to get messages from</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of messages per page</param>
        /// <param name="includeDeleted">Whether to include deleted messages</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of messages</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<MessageListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetChannelMessages(
            Guid channelId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var requestedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(requestedByIdClaim) || !Guid.TryParse(requestedByIdClaim, out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query = new GetChannelMessagesQuery(
                ChannelId: channelId,
                RequestedBy: requestedById,
                PageNumber: pageNumber,
                PageSize: pageSize,
                IncludeDeleted: includeDeleted);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Search messages in a channel by content.
        /// </summary>
        /// <param name="channelId">The channel to search in</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of results per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated search results</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<MessageListDto>),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchMessages(
            Guid channelId,
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber=1,
            [FromQuery] int pageSize=50,
            CancellationToken cancellationToken = default)
        {
            var requestedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(string.IsNullOrEmpty(requestedByIdClaim) || !Guid.TryParse(requestedByIdClaim,out var requestedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest(new { Message = "Search term is required" });
            }

            var query = new SearchMessagesQuery(
                ChannelId: channelId,
                RequestedBy: requestedById,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }





        /// <summary>
        /// Edit an existing message.
        /// Only the message sender can edit their own message.
        /// </summary>
        /// <param name="channelId">The channel containing the message</param>
        /// <param name="messageId">The message to edit</param>
        /// <param name="request">The new message content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated message</returns>
        [HttpPut("{messageId:guid}")]
        [ProducesResponseType(typeof(MessageDto),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EditMessage(
            Guid channelId,
            Guid messageId,
            [FromBody] EditMessageRequestDto request,
            CancellationToken cancellationToken)
        {
            var editedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(editedByIdClaim) || !Guid.TryParse(editedByIdClaim,out var editedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new EditMessageCommand(
                MessageId: messageId,
                EditedBy: editedById,
                NewContent: request.Content);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Message {MessageId} edited by user {EditedBy}",
                messageId,
                editedById);

            return Ok(result);
        }





        /// <summary>
        /// Delete a message (soft delete).
        /// Only the message sender can delete their own message.
        /// </summary>
        /// <param name="channelId">The channel containing the message</param>
        /// <param name="messageId">The message to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpDelete("{messageId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMessage(
            Guid channelId,
            Guid messageId,
            CancellationToken cancellationToken)
        {
            var deletedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(deletedByIdClaim) || !Guid.TryParse(deletedByIdClaim,out var deletedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new DeleteMessageCommand(
                MessageId: messageId,
                DeletedBy: deletedById);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger?.LogInformation(
                "Message {MessageId} deleted by user {DeletedBy}",
                messageId,
                deletedById);

            return Ok(result);
        }




        /// <summary>
        /// Mark a message as read.
        /// Creates a read receipt visible to other users.
        /// </summary>
        [HttpPost("{messageId:guid}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkMessageAsRead(
            Guid channelId,
            Guid messageId,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new MarkMessageAsReadCommand(messageId, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Mark all messages in a channel as read.
        /// Efficient bulk operation.
        /// </summary>
        [HttpPost("mark-all-read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllMessagesAsRead(
            Guid channelId,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new MarkChannelMessagesAsReadCommand(channelId, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Get count of unread messages in a channel.
        /// Used for unread badges in UI.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(
            Guid channelId,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var query = new GetUnreadMessageCountQuery(channelId, userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }




        /// <summary>
        /// Forward a message to another channel.
        /// </summary>
        [HttpPost("{messageId:guid}/forward")]
        [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ForwardMessage(
            Guid channelId,
            Guid messageId,
            [FromBody] ForwardMessageRequestDto request,
            CancellationToken cancellationToken)
        {
            var forwardedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(forwardedByIdClaim) || !Guid.TryParse(forwardedByIdClaim, out var forwardedById))
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var command = new ForwardMessageCommand(
                OriginalMessageId: messageId,
                TargetChannelId: request.TargetChannelId,
                ForwardedBy: forwardedById,
                AdditionalComment: request.AdditionalComment);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(
                nameof(GetMessage),
                new { channelId = request.TargetChannelId, messageId = result.Data!.Id },
                result);
        }
    }
}