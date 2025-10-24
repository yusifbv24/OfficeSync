using MediatR;
using MessagingService.Application.Attachments;
using MessagingService.Application.Commands.Attachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MessagingService.API.Controllers;

/// <summary>
/// Controller for message attachment operations.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/channels/{channelId:guid}/messages/{messageId:guid}/attachments")]
[Authorize]
[Produces("application/json")]
public class AttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(IMediator mediator, ILogger<AttachmentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }



    /// <summary>
    /// Add an attachment to a message.
    /// The file must already be uploaded to File Service.
    /// </summary>
    /// <param name="channelId">The channel containing the message</param>
    /// <param name="messageId">The message to attach the file to</param>
    /// <param name="request">Attachment metadata from File Service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created attachment</returns>
    [HttpPost]
    [ProducesResponseType(typeof(MessageAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAttachment(
        Guid channelId,
        Guid messageId,
        [FromBody] AddAttachmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var addedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(addedByIdClaim) || !Guid.TryParse(addedByIdClaim, out var addedById))
        {
            return Unauthorized(new { Message = "Invalid token" });
        }

        var command = new AddAttachmentCommand(
            MessageId: messageId,
            FileId: request.FileId,
            FileName: request.FileName,
            FileUrl: request.FileUrl,
            FileSize: request.FileSize,
            MimeType: request.MimeType,
            AddedBy: addedById);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        _logger.LogInformation(
            "Attachment {FileId} added to message {MessageId} by user {AddedBy}",
            request.FileId,
            messageId,
            addedById);

        return CreatedAtAction(
            nameof(MessagesController.GetMessage),
            "Messages",
            new { channelId, messageId },
            result);
    }




    /// <summary>
    /// Remove an attachment from a message.
    /// Only the message sender can remove attachments.
    /// </summary>
    /// <param name="channelId">The channel containing the message</param>
    /// <param name="messageId">The message containing the attachment</param>
    /// <param name="attachmentId">The attachment to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAttachment(
        Guid channelId,
        Guid messageId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var removedByIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(removedByIdClaim) || !Guid.TryParse(removedByIdClaim, out var removedById))
        {
            return Unauthorized(new { Message = "Invalid token" });
        }

        var command = new RemoveAttachmentCommand(
            MessageId: messageId,
            AttachmentId: attachmentId,
            RemovedBy: removedById);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        _logger?.LogInformation(
            "Attachment {AttachmentId} removed from message {MessageId} by user {RemovedBy}",
            attachmentId,
            messageId,
            removedById);

        return Ok(result);
    }
}