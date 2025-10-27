namespace MessagingService.Application.Attachments
{
    public record FileAttachmentDto
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; }=string.Empty;
        public string ContentType { get; init; }= string.Empty;
        public long SizeInBytes { get; init; }
        public string DownloadUrl { get; init; }=string.Empty;
        public string? ThumbnailUrl { get; init; }
        public DateTime UploadedAt { get; init; }
    }
}