using ChannelService.Domain.Common;
using ChannelService.Domain.Enums;

namespace ChannelService.Domain.Entities
{
    /// <summary>
    /// Entity representing a channel member.
    /// Part of the Channel aggregate - not an aggragate root itself.
    /// </summary>
    public class ChannelMember:BaseEntity
    {
        public Guid ChannelId { get;private set;  }
        public Guid UserId { get;private set; }
        public MemberRole Role { get; private set;  }
        public Guid AddedBy { get; private set;  }
        public DateTime JoinedAt { get; private set; }
        public bool IsRemoved { get; private set; }
        public DateTime? RemovedAt {  get; private set; }
        public Guid? RemovedBy {  get; private set; }


        // Navigation property
        public Channel Channel { get; private set; } = null!;


        // Private constructor for EF Core
        private ChannelMember() { }



        /// <summary>
        /// Entity representing a channel member.
        /// Part of the Channel aggregate - not an aggregate root itself.
        /// </summary>
        public static ChannelMember Create(
            Guid channelId,
            Guid userId,
            MemberRole role,
            Guid addedBy)
        {
            return new ChannelMember
            {
                ChannelId = channelId,
                UserId = userId,
                Role = role,
                AddedBy = addedBy,
                JoinedAt = DateTime.UtcNow,
                IsRemoved = false
            };
        }




        /// <summary>
        /// Change the member's role
        /// </summary>
        public void ChangeRole(MemberRole newRole)
        {
            if (IsRemoved)
                throw new InvalidOperationException("Cannot change role of removed member");

            Role = newRole;
            UpdateTimestamp();
        }




        /// <summary>
        /// Remove the member from the channel.
        /// </summary>
        public void Remove(Guid removedBy)
        {
            if (IsRemoved)
                throw new InvalidOperationException("Member is already removed");

            IsRemoved = true;
            RemovedAt= DateTime.UtcNow;
            RemovedBy = removedBy;
            UpdateTimestamp();
        }

        public void Restore(Guid addedBy,MemberRole role)
        {
            if(!IsRemoved)
                throw new InvalidOperationException("Cannot restore a member who is not removed");

            IsRemoved = false;
            RemovedAt = null;
            RemovedBy = null;
            Role=role;
            UpdateTimestamp();
        }
    }
}