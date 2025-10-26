using AutoMapper;
using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using FileService.Domain.Common;
using FileService.Domain.Enums;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries
{
    /// <summary>
    /// Query to retrieve detailed metadata for a specific file by its ID.
    /// This query checks access permissions before returning the file information,
    /// ensuring users can only see files they have permission to access.
    /// </summary>
    public record GetFileByIdQuery(
        Guid FileId,
        Guid RequesterId
    ): IRequest<Result<FileDto>>;


    public class GetFileByIdQueryValidator : AbstractValidator<GetFileByIdQuery>
    {
        public GetFileByIdQueryValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty()
                .WithMessage("File ID is required");

            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("Requester ID is required");
        }
    }



    /// <summary>
    /// Handles retrieving a single file by ID with permission checks.
    /// This handler verifies the requester has permission to view the file
    /// before returning its metadata. Permission rules are:
    /// - Admins can view any file
    /// - File owners can view their own files
    /// - Public files can be viewed by anyone
    /// - ChannelMembers files require channel membership verification
    /// - Restricted files require explicit access grant
    /// </summary>
    public class GetFileByIdQueryHandler:IRequestHandler<GetFileByIdQuery, Result<FileDto>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IChannelServiceClient _channelServiceClient;
        private readonly IMapper _mapper;

        public GetFileByIdQueryHandler(
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

        public async Task<Result<FileDto>> Handle(
            GetFileByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Step 1. Retrieve the file with access records
            var file = await _fileRepository.GetByIdWithAccessesAsync(
                request.FileId,
                cancellationToken);

            if(file == null || file.IsDeleted)
            {
                return Result<FileDto>.Failure("File not found");
            }

            // Step 2. Get requester's profile to check their role
            var requesterProfile = await _userServiceClient
                .GetUserProfileAsync(request.RequesterId, cancellationToken);

            if(requesterProfile == null)
            {
                return Result<FileDto>.Failure("Requester not found");
            }

            // Step 3. Check if user can access the file
            bool isAdmin = requesterProfile.Role == "Admin";

            // For channelMembers access level, verify channel membership
            if(file.AccessLevel==FileAccessLevel.ChannelMembers && file.ChannelId.HasValue)
            {
                var isMember = await _channelServiceClient
                    .IsUserChannelMemberAsync(file.ChannelId.Value, request.RequesterId, cancellationToken);

                if(!isMember && !isAdmin && file.UploadedBy != request.RequesterId)
                {
                    return Result<FileDto>.Failure("You do not have permission to view this file");
                }
            }
            else
            {
                // For other access levels, use domain logic
                bool canAccess = file.CanUserAccess(request.RequesterId, isAdmin);
                if (!canAccess)
                {
                    return Result<FileDto>.Failure("You do not have permission to view this file");
                }
            }

            // Step 4. Get uploader display name for DTO
            var uploaderProfile = await _userServiceClient
                .GetUserProfileAsync(file.UploadedBy, cancellationToken);

            // Step 5. Map to DTO and return
            var fileDto = _mapper.Map<FileDto>(file);
            fileDto.UploaderDisplayName = uploaderProfile?.DisplayName ?? "Unknown";

            return Result<FileDto>.Success(fileDto);
        }
    }
}