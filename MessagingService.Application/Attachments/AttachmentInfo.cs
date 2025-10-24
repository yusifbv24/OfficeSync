namespace MessagingService.Application.Attachments
{
    /// <summary>
    /// Attachment information from File Service.
    /// Include this when sending a message with files.
    /// </summary>
    public record AttachmentInfo(
        Guid FileId,
        string FileName,
        string FileUrl,
        long FileSize,
        string MimeType
    );
}