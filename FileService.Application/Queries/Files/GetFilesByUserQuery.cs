using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries.Files
{
    public record GetFilesByUserQuery(
        Guid UserId,
        Guid RequestedBy,
        int PageNumber=1,
        int PageSize=20
    ): IRequest<Result<PagedResult<FileListDto>>>;



    public class GetFilesByUserQueryValidator : AbstractValidator<GetFilesByUserQuery>
    {
        public GetFilesByUserQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requested user ID is required");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
        }
    }


    public class GetFilesByUserQueryHandler:IRequestHandler<GetFilesByUserQuery, Result<PagedResult<FileListDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetFilesByUserQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork=unitOfWork;
        }

        public async Task<Result<PagedResult<FileListDto>>> Handle(
            GetFilesByUserQuery request,
            CancellationToken cancellationToken)
        {
            // Users can only view their own files unless they are admins
            // For simplicity, we only allow users to query their own files
            if (request.UserId != request.RequestedBy)
            {
                return Result<PagedResult<FileListDto>>.Failure("You can only view your own files");
            }

            // Get all files uploaded by this user
            var allFiles = await _unitOfWork.FileMetadata.FindAsync(
                f => f.UploadedBy == request.UserId &&
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
                    ThumbnailUrl: f.ThumbnailPath != null ? $"/api/files/{f.Id}/thumbnail" : null);
            }).ToList();

            var pagedResult = PagedResult<FileListDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize);
        }
    }
}