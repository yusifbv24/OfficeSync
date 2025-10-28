namespace FileService.Application.DTOs.Files
{
    public record DownloadFileResponseDto(
        Stream FileStream,
        string FileName,
        string ContentType,
        long FileSize);
}