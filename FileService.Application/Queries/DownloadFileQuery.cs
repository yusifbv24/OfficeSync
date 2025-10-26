using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries
{
    /// <summary>
    /// Query to download the actual file content.
    /// This query verifies permissions and returns both the file metadata
    /// and a stream containing the file's binary content for download.
    /// 
    /// The download process also increments the file's download counter
    /// for analytics and usage tracking purposes.
    /// </summary>
    public record DownloadFileQuery(
        Guid FileId,
        Guid RequesterId
    ): IRequest<Result<FileDownloadDto>>;


    public class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
    {
        public DownloadFileQueryValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("Requester ID is required");
        }
    }


    public class DownloadFileQueryHandler:IRequestHandler<DownloadFileQuery , Result<FileDownloadDto>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IUnitOfWork _unitOfWork;

        public DownloadFileQueryHandler(
            IFileRepository fileRepository,
            IFileStorageService fileStorageService,
            IUserServiceClient userServiceClient,
            IChannelServiceClient channelServiceClient,
            IUnitOfWork unitOfWork)
        {
            _fileRepository = fileRepository;
            _fileStorageService = fileStorageService;
            _userServiceClient = userServiceClient;
            _channelServiceClient = channelServiceClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<FileDownloadDto>> Handle(
            DownloadFileQuery request,
            CancellationToken cancellationToken)
        {
            // Step 1. Retrieve the file with access records
            var file = await _fileRepository.GetByIdWithAccessesAsync(
                request.FileId,
                cancellationToken);

            if(file==null || file.IsDeleted)
            {
                return Result<FileDownloadDto>.Failure("File not found");
            }

            // Step 2: Get requester's profile
            var requesterProfile = await _userServiceClient
                .GetUserProfileAsync(request.RequesterId, cancellationToken);

            if (requesterProfile == null)
            {
                return Result<FileDownloadDto>.Failure("Requester not found");
            }

            // Step 3: Check if user can access this file
            bool isAdmin = requesterProfile.Role == "Admin";

            // For ChannelMembers access level, verify channel membership
            if (file.AccessLevel == FileAccessLevel.ChannelMembers && file.ChannelId.HasValue)
            {
                var isMember = await _channelServiceClient
                    .IsUserChannelMemberAsync(file.ChannelId.Value, request.RequesterId, cancellationToken);

                if (!isMember && !isAdmin && file.UploadedBy != request.RequesterId)
                {
                    return Result<FileDownloadDto>.Failure("You do not have permission to download this file");
                }
            }
             else
            {
                bool canAccess = file.CanUserAccess(request.RequesterId, isAdmin);

                if (!canAccess)
                {
                    return Result<FileDownloadDto>.Failure("You do not have permission to download this file");
                }
            }

             // Step 4. Check if file passed virus scanning ( if implemented)
            if(file.IsScanned.HasValue && !file.IsScanned.Value)
            {
                return Result<FileDownloadDto>.Failure("This file failed virus scanning and cannot be downloaded");

            }

            // Step 5. Get file stream from storage
            var fileStream = await _fileStorageService.GetFileStreamAsync(
                file.FilePath,
                cancellationToken);

            if (fileStream==null)
            {
                return Result<FileDownloadDto>.Failure("File content not found on storage");
            }

            // Step 6. Increment download counter for analytics
            file.IncremenetDownloadCount();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Step 7. Return download DTO with stream
            var downloadDto = new FileDownloadDto
            {
                FileId = file.Id,
                OriginalFileName = file.OriginalFileName,
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                FileStream = fileStream
            };

            return Result<FileDownloadDto>.Success(downloadDto);
        }
    }
}