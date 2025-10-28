using FileService.Domain.Enums;

namespace FileService.Application.DTOs.Files
{
    public record FileMetadataDto(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string FileSizeFormatted,
        FileType FileType,
        FileStatus Status,
        Guid UploadedBy,
        DateTime UploadedAt,
        Guid? ChannelId,
        Guid? ConversationId,
        string? Description,
        string? ThumbnailUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}