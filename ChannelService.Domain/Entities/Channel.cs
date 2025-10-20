using ChannelService.Domain.Common;
using ChannelService.Domain.Enums;
using ChannelService.Domain.Events;
using ChannelService.Domain.ValueObjects;

namespace ChannelService.Domain.Entities
{
    /// <summary>
    /// Channel aggregate root.
    /// Represents a communication channel (public, private, or direct message).
    /// Encapsulates all business logic for channel operations.
    /// </summary>
    public class Channel:BaseEntity,IAggregateRoot
    {
        private readonly List<DomainEvent> _domainEvents = new();
        private readonly List<ChannelMember> _members = new();

        // Private setters for encapsulation
        public string Name { get;private set;  }
        public string? Description { get;private set; }
        public ChannelType Type { get;private set; }
        public bool IsArchived { get; private set; }
        public Guid CreatedBy { get; private set; }

        // Navigation properties
        public IReadOnlyCollection<ChannelMember> Members => _members.AsReadOnly();
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();


        // Private constructor for EF Core
        private Channel()
        {
            Name = string.Empty;
        }

        /// <summary>
        /// Factory method to create a new channel.
        /// Ensures all business rules are enforced.
        /// </summary>
        public static Channel Create(
            ChannelName name,
            ChannelType type,
            Guid createdBy,
            string? description = null)
        {
            var channel = new Channel
            {
                Name = name,
                Type = type,
                CreatedBy = createdBy,
                Description = description,
                IsArchived = false,
                CreatedAt= DateTime.UtcNow
            };

            // Automatically add creator as owner
            var ownerMember = ChannelMember.Create(
                channelId: channel.Id,
                userId: createdBy,
                role: MemberRole.Owner,
                addedBy: createdBy);

            channel._members.Add(ownerMember);

            // Raise domain event
            channel.AddDomainEvent(new ChannelCreatedEvent(
                channel.Id,
                channel.Name,
                channel.Type,
                createdBy));

            return channel;
        }



        /// <summary>
        /// Update channel information.
        /// </summary>
        public void UpdateInfo(ChannelName? name, string? description)
        {
            if (IsArchived)
                throw new InvalidOperationException("Cannot update archived channel");

            if(name!=null)
                Name=name;

            if(description!=null)
                Description=description;

            UpdateTimestamp();
        }




        /// <summary>
        /// Add a member to the channel.
        /// Business rule: Only owners and moderators can add members.
        /// </summary>
        public void AddMember(Guid userId,Guid addedBy,MemberRole role = MemberRole.Member)
        {
            if (IsArchived)
                throw new InvalidOperationException("Cannot add members to archived channels");

            // Verify the person adding has permission
            var adder = _members.FirstOrDefault(m => m.UserId == addedBy && !m.IsRemoved);
            if(adder==null || (adder.Role!=MemberRole.Owner && adder.Role!=MemberRole.Moderator))
                throw new InvalidOperationException("Only owners and moderators can add members");

            // Check if user is already a member
            var existingMember=_members.FirstOrDefault(m=>m.UserId==userId&&!m.IsRemoved);
            if (existingMember != null)
                throw new InvalidOperationException("User is already a member of this channel");

            // Create new member
            var member = ChannelMember.Create(Id, userId, role, addedBy);
            _members.Add(member);

            // Raise domain event
            AddDomainEvent(new MemberAddedEvent(Id, userId, role, addedBy));

            UpdateTimestamp();
        }





        /// <summary>
        /// Remove a member from the channel.
        /// Business rule: Owners can remove anyone, moderators can remove members only.
        /// </summary>
        public void RemoveMember(Guid userId, Guid removedBy)
        {
            // Find the member to remove
            var member=_members.FirstOrDefault(m=>m.UserId==userId && !m.IsRemoved);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this channel");

            // Verify permissions
            var remover = _members.FirstOrDefault(m => m.UserId == removedBy && !m.IsRemoved);
            if (remover == null)
                throw new InvalidOperationException("Remover is not a member of this channel");


            // Business rules for removal
            if (member.Role == MemberRole.Owner)
            {
                if (remover.Role != MemberRole.Owner)
                    throw new InvalidOperationException("Only owners can remove other owners");

                // Check if this is the last owner
                var activeOwners = _members.Count(m => m.Role == MemberRole.Owner && !m.IsRemoved);
                if (activeOwners <= 1)
                    throw new InvalidOperationException("Cannot remove the last owner from the channel");
            }
            else if (member.Role == MemberRole.Moderator)
            {
                if(remover.Role!=MemberRole.Owner &&remover.Role!= MemberRole.Moderator)
                    throw new InvalidOperationException("Insufficient permissions to remove moderator");
            }

            // Remove the member
            member.Remove(removedBy);

            // Raise domain event
            AddDomainEvent(new MemberRemovedEvent(Id, userId, removedBy));

            UpdateTimestamp();
        }






        /// <summary>
        /// Change a member's role.
        /// Only owners can change roles.
        /// </summary>
        public void ChangeMemberRole(Guid userId,MemberRole newRole,Guid changedBy)
        {
            if (IsArchived)
                throw new InvalidOperationException("Cannot change roles in archived channel");

            var changer = _members.FirstOrDefault(m => m.UserId == changedBy && !m.IsRemoved);
            if (changer?.Role != MemberRole.Owner)
                throw new InvalidOperationException("Only owners can change member roles");

            var member=_members.FirstOrDefault(m=>m.UserId==userId && !m.IsRemoved);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this channel");

            // Dont allow changing the last owner
            if(member.Role== MemberRole.Owner && newRole != MemberRole.Owner)
            {
                var activeOwners = _members.Count(m => m.Role == MemberRole.Owner && !m.IsRemoved);
                if(activeOwners<=1)
                    throw new InvalidOperationException("Cannot change the role of the last owner");
            }

            member.ChangeRole(newRole);

            UpdateTimestamp();
        }






        /// <summary>
        /// Archive the channel.
        /// Archived channels cannot have new messages or members.
        /// </summary>
        public void Archive(Guid archivedBy)
        {
            var archiver=_members.FirstOrDefault(m=>m.UserId==archivedBy && !m.IsRemoved);

            if(archiver?.Role!=MemberRole.Owner)
                throw new InvalidOperationException("Only owners can archive channels");

            IsArchived = true;
            UpdateTimestamp();
        }





        /// <summary>
        /// Unarchive the channel.
        /// </summary>
        public void Unarchive(Guid unarchivedBy)
        {
            var unarchiver = _members.FirstOrDefault(m => m.UserId == unarchivedBy && !m.IsRemoved);
            if (unarchiver?.Role != MemberRole.Owner)
                throw new InvalidOperationException("Only owners can unarchive channels");

            IsArchived = false;
            UpdateTimestamp();
        }





        /// <summary>
        /// Check if a user is a member of this channel.
        /// </summary>
        public bool IsMember(Guid userId)
        {
            return _members.Any(m=>m.UserId==userId && !m.IsRemoved);
        }






        /// <summary>
        /// Get a member's role in the channel.
        /// </summary>
        public MemberRole? GetMemberRole(Guid userId)
        {
            return _members.FirstOrDefault(m => m.UserId == userId && !m.IsRemoved)?.Role;
        }





        private void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }


        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}