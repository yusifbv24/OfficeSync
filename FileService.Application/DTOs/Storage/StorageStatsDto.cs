namespace FileService.Application.DTOs.Storage
{
    public record StorageStatsDto(
        int TotalFiles,
        long TotalSizeBytes,
        string TotalSizeFormatted,
        int ImageCount,
        int DocumentCount,
        int VideoCount,
        int AudioCount,
        int OtherCount);
}