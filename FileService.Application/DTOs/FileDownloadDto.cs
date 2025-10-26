namespace FileService.Application.DTOs
{
    public record FileDownloadDto
    {
        public Guid FileId { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public string ContentType { get;init;  } = string.Empty;

        public long SizeInBytes { get; init; }
        public Stream FileStream { get; init; } = null!;
    }
}