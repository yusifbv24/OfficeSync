namespace ChannelService.Domain.Enums
{
    ///<summary>
    /// Defines the type of channel
    /// </summary>
    /// 
    public enum ChannelType
    {
        /// <summary>
        /// Public channel - visible to all users in the system.
        /// </summary>
        Public = 1,

        /// <summary>
        /// Private channel - only visible to members.
        /// </summary>
        Private = 2,

        /// <summary>
        /// Direct message - one-on-one communication between two users.
        /// </summary>
        DirectMessage = 3
    }
}