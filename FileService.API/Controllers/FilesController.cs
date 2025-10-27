using FileService.Application.Commands;
using FileService.Application.DTOs;
using FileService.Application.Queries;
using FileService.Domain.Common;
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




        /// <summary>
        /// Batch endpoint to get multiple file details in a single request.
        /// 
        /// PERFORMANCE CRITICAL:
        /// This endpoint is essential for the Messaging Service to efficiently
        /// fetch file details when displaying messages with attachments.
        /// 
        /// Without this, displaying 50 messages with 100 total attachments would
        /// require 100 separate HTTP calls. With this endpoint, it's just ONE call.
        /// 
        /// HTTP GET /api/files/batch?ids=guid1&ids=guid2&ids=guid3
        /// 
        /// Returns only files the requester has access to.
        /// Files that don't exist or are inaccessible are silently omitted.
        /// </summary>
        [HttpGet("batch/all")]
        [ProducesResponseType(typeof(Result<List<FileDto>>), 200)]
        public async Task<IActionResult> GetFilesBatch(
            [FromQuery] List<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetUserIdFromClaims();

                // Remove duplicates and empty GUIDs
                var distinctIds = ids.Where(id => id != Guid.Empty).Distinct().ToList();

                if (!distinctIds.Any())
                {
                    return Ok(new
                    {
                        IsSuccess = true,
                        Data = new List<FileDto>(),
                        Message = "No valid file IDs provided"
                    });
                }

                // Limit batch size to prevent abuse
                if (distinctIds.Count > 100)
                {
                    return BadRequest(new
                    {
                        IsSuccess = false,
                        Message = "Maximum 100 files per batch request"
                    });
                }

                var query = new GetFilesBatchQuery(distinctIds, userId);
                var result = await _mediator.Send(query, cancellationToken);

                if (!result.IsSuccess)
                {
                    return BadRequest(new
                    {
                        IsSuccess = false,
                        Message = result.Error
                    });
                }

                return Ok(new
                {
                    IsSuccess = true,
                    Data = result.Value,
                    Message = $"Retrieved {result.Value?.Count ?? 0} files"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Message = "An error occurred processing the request"
                });
            }
        }




        /// <summary>
        /// Link a file to a message (called by Messaging Service).
        /// 
        /// OPTIONAL ENDPOINT:
        /// This allows File Service to track which files are attached to which messages.
        /// Useful for:
        /// - Showing "attached to message in channel X" in file listings
        /// - Preventing deletion of files that are referenced in messages
        /// - Analytics about file usage
        /// 
        /// HTTP POST /api/files/{id}/link-message
        /// Body: { "messageId": "guid", "channelId": "guid" }
        /// </summary>
        [HttpPost("{id:guid}/link-message")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> LinkFileToMessage(
            Guid id,
            [FromBody] LinkFileToMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetUserIdFromClaims();

                var command = new LinkFileToMessageCommand(
                    FileId: id,
                    MessageId: request.MessageId,
                    ChannelId: request.ChannelId,
                    UserId: userId);

                var result = await _mediator.Send(command, cancellationToken);

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.Error });
                }

                return Ok(new { message = "File linked to message successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred" });
            }
        }




        /// <summary>
        /// Get metadata for multiple files by their IDs (batch operation for performance)
        /// This is called by the Messaging Service when displaying messages with attachments
        /// </summary>
        /// <param name="fileIds">Comma-separated list of file IDs (e.g., "123,456,789")</param>
        /// <returns>List of file metadata DTOs</returns>
        [HttpGet("batch")]
        [ProducesResponseType(typeof(List<FileMetadataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<FileMetadataDto>>> GetFilesByIds(
            [FromQuery] string fileIds)
        {
            try
            {
                // Parse the comma-separated file IDs
                if (string.IsNullOrWhiteSpace(fileIds))
                {
                    return BadRequest(new { error = "fileIds parameter is required" });
                }

                var fileIdsList = fileIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => Guid.TryParse(id, out _))
                    .Select(Guid.Parse)
                    .ToList();

                if (!fileIdsList.Any())
                {
                    return BadRequest(new { error = "No valid file IDs provided" });
                }

                // Limit batch size to prevent abuse
                if (fileIdsList.Count > 100)
                {
                    return BadRequest(new { error = "Maximum 100 files per batch request" });
                }

                var query = new GetFilesByIdsQuery
                {
                    FileIds = fileIdsList,
                    RequestingUserId = GetUserIdFromClaims()
                };

                var result = await _mediator.Send(query);


                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving file metadata" });
            }
        }



        /// <summary>
        /// Validate that a user has access to multiple files
        /// This is called by the Messaging Service before creating a message with file attachments
        /// </summary>
        /// <param name="request">Validation request with file IDs and user ID</param>
        /// <returns>Validation result indicating which files are accessible</returns>
        [HttpPost("validate-access")]
        [ProducesResponseType(typeof(ValidateFileAccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ValidateFileAccessResponse>> ValidateFileAccess(
            [FromBody] ValidateFileAccessRequest request)
        {
            try
            {
                if (request == null || !request.FileIds.Any())
                {
                    return BadRequest(new { error = "FileIds are required" });
                }

                // Limit batch size to prevent abuse
                if (request.FileIds.Count > 100)
                {
                    return BadRequest(new { error = "Maximum 100 files per validation request" });
                }

                var command = new ValidateFileAccessCommand
                {
                    FileIds = request.FileIds,
                    RequestingUserId = request.UserId ?? GetUserIdFromClaims(),
                    ChannelId = request.ChannelId // Optional: for channel-specific validation
                };

                var result = await _mediator.Send(command);

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while validating file access" });
            }
        }

        /// <summary>
        /// Get basic metadata for a single file (lightweight endpoint)
        /// This can be used by any service that needs to check file existence and basic info
        /// </summary>
        /// <param name="fileId">The file ID</param>
        /// <returns>File metadata</returns>
        [HttpGet("{fileId}/metadata")]
        [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<FileMetadataDto>> GetFileMetadata(Guid fileId)
        {
            try
            {
                var query = new GetFileMetadataQuery
                {
                    FileId = fileId,
                    RequestingUserId = GetUserIdFromClaims()
                };

                var result = await _mediator.Send(query);

                if (result == null)
                {
                    return NotFound(new { error = $"File {fileId} not found" });
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving file metadata" });
            }
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


        #region Request/Response Models
        /// <summary>
        /// Request model for validating file access
        /// </summary>
        public record ValidateFileAccessRequest
        {
            /// <summary>
            /// List of file IDs to validate
            /// </summary>
            public List<Guid> FileIds { get; set; } = new();

            /// <summary>
            /// Optional: User ID to validate access for (if not provided, uses current user from JWT)
            /// </summary>
            public Guid? UserId { get; set; }

            /// <summary>
            /// Optional: Channel ID for channel-specific file access validation
            /// </summary>
            public Guid? ChannelId { get; set; }
        }

        /// <summary>
        /// Response model for file access validation
        /// </summary>
        public record ValidateFileAccessResponse
        {
            /// <summary>
            /// List of file IDs that the user has access to
            /// </summary>
            public List<Guid> AccessibleFileIds { get; set; } = new();

            /// <summary>
            /// List of file IDs that the user does NOT have access to
            /// </summary>
            public List<Guid> InaccessibleFileIds { get; set; } = new();

            /// <summary>
            /// Overall validation result
            /// </summary>
            public bool AllFilesAccessible => !InaccessibleFileIds.Any();

            /// <summary>
            /// Optional validation message
            /// </summary>
            public string? Message { get; set; }
        }

        /// <summary>
        /// DTO for file metadata (returned by batch and metadata endpoints)
        /// </summary>
        public record FileMetadataDto
        {
            public Guid FileId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string FileUrl { get; set; } = string.Empty;
            public string? ThumbnailUrl { get; set; }
            public long FileSize { get; set; }
            public string MimeType { get; set; } = string.Empty;
            public string? FileHash { get; set; }
            public DateTime UploadedAt { get; set; }
            public Guid UploadedBy { get; set; }
            public bool IsDeleted { get; set; }

            /// <summary>
            /// Access level: Private, Public, ChannelMembers, Restricted
            /// </summary>
            public string AccessLevel { get; set; } = "Private";
        }
        #endregion
    }
}