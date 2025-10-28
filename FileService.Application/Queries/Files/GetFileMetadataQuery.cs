using FileService.Application.Common;
using FileService.Application.DTOs.Files;
using FileService.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace FileService.Application.Queries.Files
{
    public record GetFileMetadataQuery(
        Guid FileId,
        Guid RequestedBy
    ):IRequest<Result<FileMetadataDto>>;



    public class GetFileMetadataQueryValidator : AbstractValidator<GetFileMetadataQuery>
    {
        public GetFileMetadataQueryValidator()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("RequestedBy user ID is required");
        }
    }


    public class GetFileMetadataQueryHandler : IRequestHandler<GetFileMetadataQuery, Result<FileMetadataDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChannelServiceClient _channelServiceClient;

        public GetFileMetadataQueryHandler(
            IUnitOfWork unitOfWork,
            IChannelServiceClient channelServiceClient)
        {
            _unitOfWork= unitOfWork;
            _channelServiceClient= channelServiceClient;
        }

        public async Task<Result<FileMetadataDto>> Handle(
            GetFileMetadataQuery request,
            CancellationToken cancellationToken)
        {
            var fileMetadata = await _unitOfWork.FileMetadata.GetByIdAsync(
                request.FileId,
                cancellationToken);

            if (fileMetadata == null)
            {
                return Result<FileMetadataDto>.Failure("File not found");
            }

            // Check permissions
            bool isChannelMember = false;
            bool isConversationParticipant=false;

            if (fileMetadata.ChannelId.HasValue)
            {
                var memberCheck = await _channelServiceClient.IsUserChannelMemberAsync(
                    fileMetadata.ChannelId.Value,
                    request.RequestedBy,
                    cancellationToken);

                isChannelMember = memberCheck.IsSuccess && memberCheck.Data;
            }


            // For conversation, we would check with Message Service
            // For now, assume the uploader and participants have access
            if (fileMetadata.ConversationId.HasValue)
            {
                isConversationParticipant = true; // Placeholder - would verify with Message Service
            }

            // Use domain logic to check access
            if (!fileMetadata.CanBeAccessedBy(request.RequestedBy, isChannelMember, isConversationParticipant))
            {
                return Result<FileMetadataDto>.Failure("Access denied to this file");
            }

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

            return Result<FileMetadataDto>.Success(dto);
        }
    }
}