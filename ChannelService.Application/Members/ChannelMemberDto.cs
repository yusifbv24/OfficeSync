using ChannelService.Domain.Enums;

namespace ChannelService.Application.Members
{
    /// <summary>
    /// Channel member information
    /// </summary>
    public record ChannelMemberDto(
        Guid UserId,
        MemberRole Role,
        DateTime JoinedAt,
        Guid AddedBy);
}