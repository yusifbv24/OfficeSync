using AutoMapper;
using FileService.Application.Common;
using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries
{
    /// <summary>
    /// Query to retrieve multiple files with filtering and pagination.
    /// This query supports various filters to help users find specific files,
    /// such as filtering by channel, uploader, content type, or date range.
    /// All results are permission-filtered to ensure users only see files they can access.
    /// </summary>
    public record GetFilesQuery(
        Guid RequesterId,
        Guid? ChannelId,
        Guid? UploadedBy,
        string? ContentType,
        string? SearchTerm,
        DateTime? FromDate,
        DateTime? ToDate,
        int PageNumber,
        int PageSize
    ): IRequest<Result<PagedResult<FileDto>>>;


    public class GetFilesQueryValidator : AbstractValidator<GetFilesQuery>
    {
        public GetFilesQueryValidator()
        {
            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("Requester ID is required");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.SearchTerm))
                .WithMessage("Search term cannot exceed 255 characters");

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be less than or equal to ToDate");
        }
    }


    public class GetFilesQueryHandler : IRequestHandler<GetFilesQuery, Result<PagedResult<FileDto>>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IMapper _mapper;

        public GetFilesQueryHandler(
            IFileRepository fileRepository,
            IUserServiceClient userServiceClient,
            IChannelServiceClient channelServiceClient,
            IMapper mapper)
        {
            _fileRepository = fileRepository;
            _userServiceClient = userServiceClient;
            _channelServiceClient = channelServiceClient;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<FileDto>>> Handle(
            GetFilesQuery request,
            CancellationToken cancellationToken)
        {
            // Step 1. Get requester's profile to check their role
            var requesterProfile = await _userServiceClient
                .GetUserProfileAsync(request.RequesterId, cancellationToken);

            if (requesterProfile == null)
            {
                return Result<PagedResult<FileDto>>.Failure("Requester not found");
            }

            bool isAdmin = requesterProfile.Role == "Admin";

            // Step 2. If filtering by channel, verify user is a member (unless admin)
            if(request.ChannelId.HasValue && !isAdmin)
            {
                var isMember = await _channelServiceClient
                    .IsUserChannelMemberAsync(request.ChannelId.Value, request.RequesterId, cancellationToken);
                if (!isMember)
                {
                    return Result<PagedResult<FileDto>>.Failure("You are not a member of this channel");
                }
            }


            // Step 3. Query files from repository with filters
            var pagedFiles=await _fileRepository.GetFilesAsync(
                requesterId: request.RequesterId,
                isAdmin: isAdmin,
                channelId: request.ChannelId,
                uploadedBy: request.UploadedBy,
                contentType: request.ContentType,
                searchTerm: request.SearchTerm,
                fromDate: request.FromDate,
                toDate: request.ToDate,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            // Step 4. Get uploader display names for all files in batch
            var uploaderIds = pagedFiles.Items.Select(f => f.UploadedBy).Distinct().ToList();
            var uploaderProfiles = await _userServiceClient
                .GetUserProfilesBatchAsync(uploaderIds, cancellationToken);

            // Step 5. Map to DTOs with uploader display names
            var fileDtos = pagedFiles.Items.Select(file =>
            {
                var dto = _mapper.Map<FileDto>(file);
                dto.UploaderDisplayName = uploaderProfiles
                    .FirstOrDefault(u => u.UserId == file.UploadedBy)?.DisplayName ?? "Unknown";
                return dto;
            }).ToList();

            var result = new PagedResult<FileDto>
            {
                Items = fileDtos,
                TotalCount = pagedFiles.TotalCount,
                PageNumber = pagedFiles.PageNumber,
                PageSize = pagedFiles.PageSize
            };

            return Result<PagedResult<FileDto>>.Success(result);
        }
    }
}