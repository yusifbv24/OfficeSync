using ChannelService.Domain.Enums;

namespace ChannelService.Application.Members
{
    /// <summary>
    /// Channel member information
    /// </summary>
    public record ChannelMemberDto
    {
        public Guid UserId { get; init; }
        public MemberRole Role { get; init; }
        public DateTime JoinedAt { get; init; }
        public Guid AddedBy { get; init; }
    }
}