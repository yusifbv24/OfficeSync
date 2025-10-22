namespace MessagingService.Domain.ValueObjects
{
    /// <summary>
    /// Value object for message content with validation.
    /// Immutable and defined by its value, not identity.
    /// Ensures all messages meet business rules for content.
    /// </summary>
    public class MessageContent:IEquatable<MessageContent>
    {
        public string Value { get; }
        private MessageContent(string value)
        {
            Value= value;
        }

        /// <summary>
        /// Factory method to create MessageContent with validation.
        /// Throws exceptions if validation fails - this is intentional
        /// because invalid content represents a programming error, not a user error.
        /// </summary>
        public static MessageContent Create(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("Message content cannot be empty",nameof(value));

            if (value.Length > 4000)
                throw new ArgumentException("Message content cannot exceed 4000 characters", nameof(value));
            
            return new MessageContent(value.Trim());
        }

        public bool Equals(MessageContent? other)
        {
             if(other is null) return false;
             return Value.Equals(other.Value,StringComparison.Ordinal);
        }
        public override bool Equals(object? obj) => Equals(obj as MessageContent);
        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
        public override string ToString() => Value;

        // Implicit conversion allows natural usage: string content = messageContent;
        public static implicit operator string(MessageContent content) => content.Value;
    }
}