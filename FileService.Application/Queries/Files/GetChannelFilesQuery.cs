using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries.Files
{
    public record GetChannelFilesQuery(
        Guid ChannelId,
        Guid RequestedBy,
        int PageNumber=1,
        int PageSize=20
    ):IRequest<Result<PagedResult<FileListDto>>>;

    public class GetChannelFilesQueryValidator : AbstractValidator<GetChannelFilesQuery>
    {
        public GetChannelFilesQueryValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Channel ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("RequestedBy user ID is required");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
        }
    }

    public class GetChannelFilesQueryHandler : IRequestHandler<GetChannelFilesQuery, Result<PagedResult<FileListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;

        public GetChannelFilesQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient)
        {
            _unitOfWork = unitOfWork;
            _channelServiceClient = channelServiceClient;
        }

        public async Task<Result<PagedResult<FileListDto>>> Handle(
            GetChannelFilesQuery request,
            CancellationToken cancellationToken)
        {
            // Verify user is a member of the channel
            var memberCheck = await _channelServiceClient.IsUserChannelMemberAsync(
                request.ChannelId,
                request.RequestedBy,
                cancellationToken);

            if(!memberCheck.IsSuccess || !memberCheck.Data)
            {
                return Result<PagedResult<FileListDto>>.Failure("User is not a member of this channel");
            }

            // Get all files for this channel
            var allFiles = await _unitOfWork.FileMetadata.FindAsync(
                f => f.ChannelId == request.ChannelId &&
                    !f.IsDeleted &&
                    f.Status == FileStatus.Available,
                cancellationToken);

            var filesList = allFiles.OrderByDescending(f => f.UploadedAt).ToList();
            var totalCount = filesList.Count;


            // Apply pagination
            var pagedFiles = filesList
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs
            var dtos = pagedFiles.Select(f =>
            {
                var fileSize = FileSize.Create(f.FileSizeBytes);
                return new FileListDto(
                    Id: f.Id,
                    OriginalFileName: f.OriginalFileName,
                    FileType: f.FileType,
                    FileSizeBytes: f.FileSizeBytes,
                    FileSizeFormatted: fileSize.ToHumanReadable(),
                    UploadedBy: f.UploadedBy,
                    UploadedAt: f.UploadedAt,
                    ThumbnailUrl: f.ThumbnailPath != null ? $"/api/files/{f.Id}/thubmnail" : null);
            }).ToList();

            var pagedResult = PagedResult<FileListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Result<PagedResult<FileListDto>>.Success(pagedResult);
        }
    }
}