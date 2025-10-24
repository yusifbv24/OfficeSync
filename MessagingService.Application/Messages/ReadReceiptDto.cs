namespace MessagingService.Application.Messages
{
    /// <summary>
    /// Information about who read a message and when.
    /// </summary>
    public record ReadReceiptDto(
        Guid UserId,
        string UserName,
        DateTime ReadAt
    );
}