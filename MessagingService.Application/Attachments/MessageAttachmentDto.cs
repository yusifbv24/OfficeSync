namespace MessagingService.Application.Attachments
{
    /// <summary>
    /// File attachment information.
    /// Links to files stored in the File Service.
    /// </summary>
    public record MessageAttachmentDto
    {
        public Guid Id { get; init; }
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FileUrl { get; init;  }=string.Empty;
        public long FileSize { get; init; }
        public string MimeType { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}