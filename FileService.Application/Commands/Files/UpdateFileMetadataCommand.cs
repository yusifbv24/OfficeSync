using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FileService.Application.Commands.Files
{
    public record UpdateFileMetadataCommand(
        Guid FileId,
        Guid UpdatedBy,
        string? Description
    ):IRequest<Result<FileMetadataDto>>;


    public class UpdateFileMetadataCommandValidator : AbstractValidator<UpdateFileMetadataCommand>
    {
        public UpdateFileMetadataCommandValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");
            RuleFor(x => x.UpdatedBy)
                .NotEmpty().WithMessage("UpdatedBy user ID is required");
            When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
            });
        }
    }

    public class UpdateFileMetadataCommandHandler : IRequestHandler<UpdateFileMetadataCommand, Result<FileMetadataDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateFileMetadataCommandHandler> _logger;

        public UpdateFileMetadataCommandHandler(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateFileMetadataCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<FileMetadataDto>> Handle(
            UpdateFileMetadataCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var fileMetadata = await _unitOfWork.FileMetadata.GetByIdAsync(
                    request.FileId,
                    cancellationToken);

                if (fileMetadata == null)
                {
                    return Result<FileMetadataDto>.Failure("File not found");
                }

                // Only the uploader can update metadata
                if (fileMetadata.UploadedBy != request.UpdatedBy)
                {
                    return Result<FileMetadataDto>.Failure("Only the file uploader can update file metadata");
                }

                // Update metadata using domain logic
                fileMetadata.UpdateMetadata(request.Description);

                // Log the access
                var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;
                fileMetadata.LogAccess(request.UpdatedBy, AccessType.Update, ipAddress);

                await _unitOfWork.FileMetadata.UpdateAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "File metadata updated : {FileId}, UpdatedBy: {UserId}",
                    fileMetadata.Id,
                    request.UpdatedBy);

                // Map to DTO
                var fileSize = FileSize.Create(fileMetadata.FileSizeBytes);

                var dto = new FileMetadataDto(
                                Id: fileMetadata.Id,
                                OriginalFileName: fileMetadata.OriginalFileName,
                                ContentType: fileMetadata.ContentType,
                                FileSizeBytes: fileMetadata.FileSizeBytes,
                                FileSizeFormatted: fileSize.ToHumanReadable(),
                                FileType: fileMetadata.FileType,
                                Status: fileMetadata.Status,
                                UploadedBy: fileMetadata.UploadedBy,
                                UploadedAt: fileMetadata.UploadedAt,
                                ChannelId: fileMetadata.ChannelId,
                                ConversationId: fileMetadata.ConversationId,
                                Description: fileMetadata.Description,
                                ThumbnailUrl: fileMetadata.ThumbnailPath != null ? $"/api/files/{fileMetadata.Id}/thumbnail" : null,
                                CreatedAt: fileMetadata.CreatedAt,
                                UpdatedAt: fileMetadata.UpdatedAt);

                return Result<FileMetadataDto>.Success(dto, "File metadata updated succesfully");
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "Failed to update file metadata {FileId}: {Message}", request.FileId, ex.Message);
                return Result<FileMetadataDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating file metadata {FileId}", request.FileId);
                return Result<FileMetadataDto>.Failure("An error occurred while updating file metadata");
            }
        }
    }
}