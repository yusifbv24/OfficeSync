namespace MessagingService.Application.Reactions
{
    /// <summary>
    /// Reaction information for display.
    /// Groups reactions by emoji and shows who reacted.
    /// </summary>
    public record MessageReactionDto
    {
        public string Emoji { get; init; } = string.Empty;
        public List<ReactionUserDto> Users { get; init; } = [];
        public int Count { get; init; }
    }

    /// <summary>
    /// User who reacted to a message.
    /// </summary>
    public record ReactionUserDto
    {
        public Guid UserId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}