namespace ChannelService.Domain.Enums
{
    /// <summary>
    /// Defines the role of a member within a channel.
    /// </summary>
    public enum MemberRole
    {
        ///<summary>
        /// Regular member -can read and write messages.
        /// </summary>
        Member=1,

        /// <summary>
        /// Moderator -can manage messages and members 
        /// </summary>
        Moderator=2,


        /// <summary>
        /// Owner - full control over the channel
        /// </summary>
        Owner=3
    }
}