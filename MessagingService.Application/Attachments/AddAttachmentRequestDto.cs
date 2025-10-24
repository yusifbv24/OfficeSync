namespace MessagingService.Application.Attachments;
/// <summary>
/// Request to add an attachment to a message.
/// File must already be uploaded to File Service.
/// </summary>
public record AddAttachmentRequestDto(
    Guid FileId,
    string FileName,
    string FileUrl,
    long FileSize,
    string MimeType
);