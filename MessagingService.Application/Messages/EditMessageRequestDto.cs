namespace MessagingService.Application.Messages
{
    /// <summary>
    /// Request to edit an existing message.
    /// Only content can be changed; metadata remains unchanged.
    /// </summary>
    public record EditMessageRequestDto(
        string Content);
}