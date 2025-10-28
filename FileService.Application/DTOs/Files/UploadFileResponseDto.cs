using FileService.Domain.Enums;

namespace FileService.Application.DTOs.Files
{
    public record UploadFileResponseDto(
        Guid FileId,
        string OriginalFileName,
        FileType FileType,
        long FileSizeBytes,
        string FileSizeFormatted,
        string? ThumbnailUrl,
        DateTime UploadedAt);
}