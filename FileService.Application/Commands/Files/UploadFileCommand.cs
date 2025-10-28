using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.Entities;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileService.Application.Commands.Files
{
    public record UploadFileCommand(
        Stream FileStream,
        string FileName,
        string ContentType,
        long FileSizeBytes,
        Guid UploadedBy,
        Guid? ChannelId,
        Guid? ConversationId,
        string? Description
     ):IRequest<Result<UploadFileResponseDto>>;


    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<UploadFileResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly ILogger<UploadFileCommandHandler> _logger;

        public UploadFileCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorage fileStorage,
            IThumbnailGenerator thumbnailGenerator,
            IChannelServiceClient channelServiceClient,
            ILogger<UploadFileCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _thumbnailGenerator = thumbnailGenerator;
            _channelServiceClient = channelServiceClient;
            _logger = logger;
        }


        public async Task<Result<UploadFileResponseDto>> Handle(
            UploadFileCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate the file belongs to either channel or conversation, not both
                if(request.ChannelId.HasValue && request.ConversationId.HasValue)
                {
                    return Result<UploadFileResponseDto>.Failure(
                        "File cannot belong to both a channel and  a conversation");
                }

                // If uploading to a channel, verify the user is a member
                if (request.ChannelId.HasValue)
                {
                    var isMember = await _channelServiceClient.IsUserChannelMemberAsync(
                        request.ChannelId.Value,
                        request.UploadedBy,
                        cancellationToken);

                    if(!isMember.IsSuccess || !isMember.Data)
                    {
                        return Result<UploadFileResponseDto>.Failure(
                            "User is not a member of the specified channel");
                    }
                }


                // Create value objects with validation
                var mimeType = MimeType.Create(request.ContentType);
                var fileSize = FileSize.Create(
                    request.FileSizeBytes,
                    isImage: mimeType.GetFileType() == FileType.Image);

                // Generate storage path (organized by year/month/fileId)
                var fileId = Guid.NewGuid();
                var now= DateTime.UtcNow;
                var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
                var storagePath=$"{now.Year:D4}/{now.Month:D2}/{fileId}{extension}";

                // Create file metadata entity
                var fileMetadata = FileMetadata.Create(
                    originalFileName: request.FileName,
                    contentType: mimeType,
                    fileSize: fileSize,
                    storagePath: storagePath,
                    uploadedBy: request.UploadedBy,
                    channelId: request.ChannelId,
                    conversationId: request.ConversationId,
                    description: request.Description);

                // Save to database first
                await _unitOfWork.FileMetadata.AddAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);


                // Store the physical file
                await _fileStorage.SaveFileAsync(
                    storagePath,
                    request.FileStream,
                    cancellationToken);


                // Generate thumbnail if it is an image
                string? thumbnailUrl= null;
                if (mimeType.GetFileType() == FileType.Image)
                {
                    try
                    {
                        var thumbnailPath = await _thumbnailGenerator.GenerateThumbnailAsync(
                            storagePath,
                            cancellationToken);

                        if (!string.IsNullOrWhiteSpace(thumbnailPath))
                        {
                            fileMetadata.SetThumbnail(thumbnailPath);
                            thumbnailUrl = $"/api/files/{fileMetadata.Id}/thumbnail";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate thumbnail for file {FileId}", fileMetadata.Id);
                    }
                }


                // Mark file as available
                fileMetadata.MarkAsAvailable();
                await _unitOfWork.FileMetadata.UpdateAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "File uploaded successfully: {FileId}, Name: {FileName}, Size: {FileSize}, User: {UserId}",
                    fileMetadata.Id,
                    fileMetadata.OriginalFileName,
                    fileSize.ToHumanReadable(),
                    request.UploadedBy);

                // Build response DTO
                var response = new UploadFileResponseDto(
                    FileId: fileMetadata.Id,
                    OriginalFileName: fileMetadata.OriginalFileName,
                    FileType: fileMetadata.FileType,
                    FileSizeBytes: fileMetadata.FileSizeBytes,
                    FileSizeFormatted: fileSize.ToHumanReadable(),
                    ThumbnailUrl: thumbnailUrl,
                    UploadedAt: fileMetadata.UploadedAt);

                return Result<UploadFileResponseDto>.Success(response, "File uploaded succesfully");
            }
            catch (ArgumentException ex)
            {
                // Validation errors from value objects
                _logger?.LogWarning(ex, "File upload validation failed: {Message}", ex.Message);
                return Result<UploadFileResponseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading file: {FileName}", request.FileName);
                return Result<UploadFileResponseDto>.Failure("An error occurred while uploading the file");
            }
        }
    }
}