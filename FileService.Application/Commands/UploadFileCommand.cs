using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using File = FileService.Domain.Entities.File;

namespace FileService.Application.Commands
{
    public record UploadFileCommand(
        IFormFile File,
        Guid UploadedBy,
        Guid? ChannelId,
        Guid? MessageId,
        FileAccessLevel AccessLevel,
        string? Description
    ):IRequest<Result<Guid>>;



    public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        // Maximum file size: 100MB (configurable in production via appsettings)
        private const long MaxFileSizeBytes=100*1024*1024;

        private static readonly string[] AllowedContentTypes = new[]
        {
            // Images
            "image/jpg","image/jpeg","image/png","image/gif","image/webp","image/svg+xml",
            // Documents
            "application/pdf",
            "application/msword", //.doc
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
            "application/vnd.ms-excel",// .xls
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
            "application/vnd.ms-powerpoint",// .ppt
            "application/vnd.openxmlformats-officedocument.presentationml.pressentation",// .pptx
            // Text
            "text/plain","text/csv","text/html","text/xml",
            // Archives
            "application/zip","application/x-rar-compressed","application/x-7z-compressed",
            // Video
            "video/mp4","video/mpeg","video/quicktime","video/x-msvideo",
            // Audio
            "audio/mpeg","audio/wav","audio/ogg"
        };

        public UploadFileCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File is requred");

            RuleFor(x => x.File.Length)
                .GreaterThan(0)
                .WithMessage("File cannot be empty")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage($"File size cannot exceed {MaxFileSizeBytes / 1024 / 1024}MB");

            RuleFor(x => x.File.FileName)
                .NotEmpty()
                .WithMessage("File must have a filename")
                .MaximumLength(255)
                .WithMessage("Filename cannot exceed 255 characters");

            RuleFor(x => x.File.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
                .WithMessage("File type is not allowed");

            RuleFor(x => x.UploadedBy)
                .NotEmpty()
                .WithMessage("Uploader ID is required");

            RuleFor(x => x.ChannelId)
                .NotEmpty()
                .When(x => x.AccessLevel == FileAccessLevel.ChannelMembers)
                .WithMessage("Channel ID is required when access level is ChannelMembers");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description cannot exceed 500 characters");
        }
    }



    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<Guid>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IHashService _hashService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UploadFileCommandHandler> _logger;

        public UploadFileCommandHandler(
        IFileRepository fileRepository,
        IFileStorageService fileStorageService,
        IThumbnailService thumbnailService,
        IHashService hashService,
        IUnitOfWork unitOfWork,
        ILogger<UploadFileCommandHandler> logger)
        {
            _fileRepository = fileRepository;
            _fileStorageService = fileStorageService;
            _thumbnailService = thumbnailService;
            _hashService = hashService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<Guid>> Handle(
            UploadFileCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Step 1: Generate unique filename to prevent collisions
                // Format: {Guid}_{OriginalFileName}
                var storedFileName = $"{Guid.NewGuid()}_{request.File.FileName}";

                // Step 2: Calculate file hash for integrity and potential deduplication
                // The hash is calculated before saving to detect duplicate files
                string fileHash;
                using(var stream = request.File.OpenReadStream())
                {
                    fileHash = await _hashService.ComputeHashAsync(stream, cancellationToken);
                }

                // Step 3: Check if identical file already exists (optional deduplication)
                // This can save storage space by reusing existing files
                var existingFile=await _fileRepository
                    .GetByHashAsync(fileHash, cancellationToken);

                if (existingFile!=null && !existingFile.IsDeleted)
                {
                    // File with identical content already exists
                    // You can either reuse it or allow duplicate - based on business rules
                    // For now, we'll allow duplicates but you could return existing file ID
                }

                // Step 4: Save the physical file to storage
                // The storage service handles creating directory structure and saving bytes
                var filePath=await _fileStorageService.SaveFileAsync(
                    request.File,
                    storedFileName,
                    cancellationToken);

                // Step 5: Generate thumbnail if this is an image file
                string? thumbnailPath = null;
                if (request.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(
                            filePath,
                            storedFileName,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Thumbnail generation failed. Error : {ex.Message}");
                    }
                }


                // Step 6: Create domain entity with all metadata
                var file = File.Create(
                    originalFileName: request.File.FileName,
                    storedFileName: storedFileName,
                    contentType: request.File.ContentType,
                    sizeInBytes: request.File.Length,
                    filePath: filePath,
                    fileHash: fileHash,
                    uploadedBy: request.UploadedBy,
                    accessLevel: request.AccessLevel,
                    channelId: request.ChannelId,
                    messageId: request.MessageId,
                    description: request.Description);

                // Set thumbnail if it was generated succesfully
                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    file.SetThumbnail(thumbnailPath); 
                }

                // Step 7: Save to database within a transaction
                await _fileRepository.AddAsync(file, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Step 8: Trigger async virus scanning
                // This happens asynchronously so we don't block the upload
                // A background service will process the scan and update the file entity
                // TODO: Implement virus scanning queue/background service


                // Step 9: Domain events are automatically published after SaveChanges
                // The FileUploadedEvent will be dispatched to any subscribers

                return Result<Guid>.Success(file.Id);
            }
            catch (Exception ex)
            {
                // If anything goes wrong, clean up the physical file if it was saved
                // This prevents orphaned files on disk
                // TODO: Add proper cleanup logic and logging
                return Result<Guid>.Failure($"Failed to upload file: {ex.Message}");
            }
        }
    }
}