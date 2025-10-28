using FileService.Application.Common;
using FileService.Application.DTOs.Storage;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries.Statistics
{
    public record GetUserStorageStatsQuery(
        Guid UserId,
        Guid RequestedBy
    ):IRequest<Result<StorageStatsDto>>;


    public class GetUserStorageStatsQueryValidator : AbstractValidator<GetUserStorageStatsQuery>
    {
        public GetUserStorageStatsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("RequestedBy user ID is required");
        }
    }



    public class GetUserStorageStatsQueryHandler : IRequestHandler<GetUserStorageStatsQuery, Result<StorageStatsDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserStorageStatsQueryHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork= unitOfWork;
        }

        public async Task<Result<StorageStatsDto>> Handle(
            GetUserStorageStatsQuery request,
            CancellationToken cancellationToken)
        {
            // Users can only view their own statistics
            if(request.UserId != request.RequestedBy)
            {
                return Result<StorageStatsDto>.Failure("You can only view your ")
            }

            // Get all files for this user
            var userFiles = await _unitOfWork.FileMetadata.FindAsync(
                f => f.UploadedBy == request.UserId &&
                    !f.IsDeleted &&
                    f.Status == FileStatus.Available,
                cancellationToken);

            var filesList = userFiles.ToList();

            // Calculate statistics
            var totalFiles = filesList.Count;
            var totalSizeBytes = filesList.Sum(f => f.FileSizeBytes);
            var imageCount = filesList.Count(f => f.FileType == FileType.Image);
            var documentCount = filesList.Count(f => f.FileType == FileType.Document);
            var videoCount = filesList.Count(f => f.FileType == FileType.Video);
            var audioCount = filesList.Count(f => f.FileType == FileType.Audio);
            var otherCount = filesList.Count(f => f.FileType == FileType.Other || f.FileType == FileType.Archive);

            var fileSize = totalSizeBytes > 0 ? FileSize.Create(totalSizeBytes) : null;
            var totalSizeFormatted = fileSize?.ToHumanReadable() ?? "0 B";

            var stats = new StorageStatsDto(
                TotalFiles: totalFiles,
                TotalSizeBytes: totalSizeBytes,
                TotalSizeFormatted: totalSizeFormatted,
                ImageCount: imageCount,
                DocumentCount: documentCount,
                VideoCount: videoCount,
                AudioCount: audioCount,
                OtherCount: otherCount);

            return Result<StorageStatsDto>.Success(stats);
        }
    }
}