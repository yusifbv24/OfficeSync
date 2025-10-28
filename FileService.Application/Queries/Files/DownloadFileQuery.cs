using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileService.Application.Queries.Files
{
    public record DownloadFileQuery(
        Guid FileId,
        Guid RequestedBy,
        string IpAddress,
        string? UserAgent
    ):IRequest<Result<DownloadFileResponseDto>>;


    public class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
    {
        public DownloadFileQueryValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("RequestedBy user ID is required");

            RuleFor(x => x.IpAddress)
                .NotEmpty().WithMessage("IP address is required");
        }
    }


    public class DownloadFileQueryHandler : IRequestHandler<DownloadFileQuery, Result<DownloadFileResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly ILogger<DownloadFileQueryHandler> _logger;

        public DownloadFileQueryHandler(
            IUnitOfWork unitOfWork,
            IFileStorage fileStorage,
            IChannelServiceClient channelServiceClient,
            ILogger<DownloadFileQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _channelServiceClient = channelServiceClient;
            _logger = logger;
        }

        public async Task<Result<DownloadFileResponseDto>> Handle(
            DownloadFileQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var fileMetadata = await _unitOfWork.FileMetadata.GetByIdAsync(
                    request.FileId,
                    cancellationToken);

                if (fileMetadata == null)
                {
                    return Result<DownloadFileResponseDto>.Failure("File not found");
                }

                // Check permissions
                bool isChannelMember = false;
                bool isConversationParticipant = false;

                if (fileMetadata.ChannelId.HasValue)
                {
                    var memberCheck = await _channelServiceClient.IsUserChannelMemberAsync(
                        fileMetadata.ChannelId.Value,
                        request.RequestedBy,
                        cancellationToken);

                    isChannelMember = memberCheck.IsSuccess && memberCheck.Data;
                }

                if (fileMetadata.ConversationId.HasValue)
                {
                    isConversationParticipant = true; // Placeholder
                }

                // Use domain logic to check access
                if (!fileMetadata.CanBeAccessedBy(request.RequestedBy, isChannelMember, isConversationParticipant))
                {
                    _logger?.LogWarning(
                        "Access denied to file {FileId} for user {UserId}",
                        request.FileId,
                        request.RequestedBy);

                    return Result<DownloadFileResponseDto>.Failure("Access denied to this file");
                }

                // Log the access
                fileMetadata.LogAccess(
                    request.RequestedBy,
                    AccessType.Download,
                    request.IpAddress,
                    request.UserAgent);

                await _unitOfWork.FileMetadata.UpdateAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get file stream from storage
                var fileStream = await _fileStorage.GetFileStreamAsync(
                    fileMetadata.StoragePath,
                    cancellationToken);

                _logger?.LogInformation(
                    "File downloaded: {FileId}, Name: {FileName} , User: {UserId}",
                    fileMetadata.Id,
                    fileMetadata.OriginalFileName,
                    request.RequestedBy);

                var response = new DownloadFileResponseDto(
                    FileStream: fileStream,
                    FileName: fileMetadata.OriginalFileName,
                    ContentType: fileMetadata.ContentType,
                    FileSize: fileMetadata.FileSizeBytes);

                return Result<DownloadFileResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading file {FileId}", request.FileId);
                return Result<DownloadFileResponseDto>.Failure("An error occurred while downloading the file");
            }
        }
    }
}