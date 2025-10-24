namespace MessagingService.Application.Reactions
{
    /// <summary>
    /// Request to add a reaction (emoji) to a message.
    /// </summary>
    public record ReactionRequestDto(
        string Emoji);
}