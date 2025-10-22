namespace MessagingService.Domain.Enums
{
    /// <summary>
    /// Defines the type of message content.
    /// Used to determine how to render and process the message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Plain text message.
        /// </summary>
        Text = 1,

        /// <summary>
        /// Message with file attachments.
        /// </summary>
        File = 2,

        /// <summary>
        /// System-generated message (user joined, channel created, etc).
        /// </summary>
        System = 3,

        /// <summary>
        /// Message containing an image.
        /// </summary>
        Image = 4
    }
}