using FileService.Application.Commands;
using FileService.Application.DTOs;
using FileService.Application.Queries;
using FileService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController:ControllerBase
    {
        private readonly IMediator _mediator;

        public FilesController(IMediator mediator)
        {
            _mediator= mediator;
        }


        /// <summary>
        /// This endpoint accepts multipart/form-data with the file and metadata.
        /// The file is saved to storage, metadata is stored in the database,
        /// and a thumbnail is generated if it's an image.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(104857600)] //100 Mb max file size
        public async Task<IActionResult> UploadFile(
            UploadFileRequest request,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new UploadFileCommand(
                File: request.File,
                UploadedBy: userId,
                ChannelId: request.ChannelId,
                MessageId: request.MessageId,
                AccessLevel: request.AccessLevel,
                Description: request.Description);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }

            return CreatedAtAction(
                nameof(GetFileById),
                new { id = result.Value },
                new { fileId = result.Value });
        }




        /// <summary>
        /// Retrieves file metadata by ID.
        /// 
        /// HTTP GET /api/files/{id}
        /// 
        /// Returns detailed metadata about the file including name, size, upload date,
        /// access level, etc. Does NOT return the actual file content - use the
        /// download endpoint for that.
        /// 
        /// Permission check: User must have access to view this file based on its
        /// access level and their role.
        /// 
        /// Returns: 200 OK with FileDto, or 404 if file not found or access denied
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFileById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var query = new GetFileByIdQuery(id, userId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }
            return Ok(result);
        }



        /// <summary>
        /// Retrieves a paginated list of files with optional filtering.
        /// 
        /// HTTP GET /api/files
        /// 
        /// Query parameters:
        /// - channelId: Filter by channel
        /// - uploadedBy: Filter by uploader
        /// - contentType: Filter by content type (e.g., "image/")
        /// - searchTerm: Search in filenames
        /// - fromDate: Filter files uploaded after this date
        /// - toDate: Filter files uploaded before this date
        /// - pageNumber: Page number (default 1)
        /// - pageSize: Items per page (default 20, max 100)
        /// 
        /// Returns: 200 OK with PagedResult<FileDto>
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFiles(
            [FromQuery] Guid? channelId,
            [FromQuery] Guid? uploadedBy,
            [FromQuery] string? contentType,
            [FromQuery] string? searchTerm,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageNumber=1,
            [FromQuery] int pageSize=20,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var query = new GetFilesQuery(
                RequesterId: userId,
                ChannelId: channelId,
                UploadedBy: uploadedBy,
                ContentType: contentType,
                SearchTerm: searchTerm,
                FromDate: fromDate,
                ToDate: toDate,
                PageNumber: pageNumber,
                PageSize: pageSize);
            
            var result=await _mediator.Send(query,cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(result);
        }




        /// <summary>
        /// Downloads the actual file content.
        /// 
        /// HTTP GET /api/files/{id}/download
        /// 
        /// This endpoint streams the file content directly to the client.
        /// The file is returned with appropriate Content-Type and Content-Disposition
        /// headers so browsers know how to handle it (display inline or download).
        /// 
        /// Permission check: User must have access to download this file.
        /// 
        /// The download counter is incremented for analytics.
        /// 
        /// Returns: 200 OK with file stream, or 404 if file not found or access denied
        /// </summary>
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> DownloadFile(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var query=new DownloadFileQuery(id, userId);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return NotFound(new { error = result.Error });
            }

            var downloadDto = result.Value!;

            // Return file stream with proper headers
            // Content-Disposition tells the browser to download the file
            // FileContentResult automatically handles streaming large files efficiently
            return File(
                downloadDto.FileStream,
                downloadDto.ContentType,
                downloadDto.OriginalFileName);
        }





        /// <summary>
        /// Downloads the thumbnail image for a file.
        /// 
        /// HTTP GET /api/files/{id}/thumbnail
        /// 
        /// Returns a smaller preview image for image files.
        /// Only works for files that have thumbnails (images).
        /// 
        /// Returns: 200 OK with thumbnail image, or 404 if no thumbnail exists
        /// </summary>
        [HttpGet("{id:guid}/thumbnail")]
        public async Task<IActionResult> GetThumbnail(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            // Similar implementation to DownloadFile but for thumbnails

            return NotFound(new { error = "Thumbnail not found" });
        }




        /// <summary>
        /// Soft-deletes a file.
        /// 
        /// HTTP DELETE /api/files/{id}
        /// 
        /// The file is marked as deleted but not physically removed from storage.
        /// This allows for recovery if the deletion was accidental.
        /// 
        /// Permission check: Only file owner, channel operators, or admins can delete files.
        /// 
        /// Returns: 200 OK on success, 404 if file not found, 403 if access denied
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFile(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command=new DeleteFileCommand(id, userId);
            var result=await _mediator.Send(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(new {error=result.Error});
            }
            return Ok(new { message = "File deleted succesfully" });
        }



        /// <summary>
        /// Restores a previously deleted file.
        /// 
        /// HTTP POST /api/files/{id}/restore
        /// 
        /// Permission check: Only file owner or admins can restore files.
        /// 
        /// Returns: 200 OK on success, 404 if file not found, 403 if access denied
        /// </summary>
        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> RestoreFile(
        Guid id,
        CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new RestoreFileCommand(id, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "File restored successfully" });
        }



        /// <summary>
        /// Updates a file's description.
        /// 
        /// HTTP PATCH /api/files/{id}/description
        /// 
        /// Request body: { "description": "New description text" }
        /// 
        /// Permission check: Only file owner or admins can update description.
        /// 
        /// Returns: 200 OK on success
        /// </summary>
        [HttpPatch("{id:guid}/description")]
        public async Task<IActionResult> UpdateDescription(
        Guid id,
        [FromBody] UpdateDescriptionRequest request,
        CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new UpdateFileDescriptionCommand(id, request.Description, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Description updated successfully" });
        }




        /// <summary>
        /// Changes a file's access level.
        /// 
        /// HTTP PATCH /api/files/{id}/access-level
        /// 
        /// Request body: { "accessLevel": "Public" }
        /// Possible values: Private, Public, ChannelMembers, Restricted
        /// 
        /// Permission check: Only file owner or admins can change access level.
        /// 
        /// Returns: 200 OK on success
        /// </summary>
        [HttpPatch("{id:guid}/access-level")]
        public async Task<IActionResult> UpdateAccessLevel(
        Guid id,
        [FromBody] UpdateAccessLevelRequest request,
        CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new UpdateFileAccessLevelCommand(id, request.AccessLevel, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Access level updated successfully" });
        }



        /// <summary>
        /// Grants explicit access to a restricted file for a specific user.
        /// 
        /// HTTP POST /api/files/{id}/access
        /// 
        /// Request body: { "userId": "guid" }
        /// 
        /// Only works when the file's access level is Restricted.
        /// Permission check: Only file owner or admins can grant access.
        /// 
        /// Returns: 200 OK on success
        /// </summary>
        [HttpPost("{id:guid}/access")]
        public async Task<IActionResult> GrantAccess(
        Guid id,
        [FromBody] GrantAccessRequest request,
        CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new GrantFileAccessCommand(id, request.UserId, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Access granted successfully" });
        }




        /// <summary>
        /// Revokes previously granted access to a file.
        /// 
        /// HTTP DELETE /api/files/{id}/access/{userId}
        /// 
        /// Permission check: Only file owner or admins can revoke access.
        /// 
        /// Returns: 200 OK on success
        /// </summary>
        [HttpDelete("{id:guid}/access/{targetUserId:guid}")]
        public async Task<IActionResult> RevokeAccess(
            Guid id,
            Guid targetUserId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserIdFromClaims();

            var command = new RevokeFileAccessCommand(id, targetUserId, userId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Access revoked successfully" });
        }



        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return userId;
        }
    }
}