using FileService.Domain.Enums;

namespace FileService.Application.DTOs.Files
{
    public record FileListDto(
        Guid Id,
        string OriginalFileName,
        FileType FileType,
        long FileSizeBytes,
        string FileSizeFormatted,
        Guid UploadedBy,
        DateTime UploadedAt,
        string? ThumbnailUrl);
}