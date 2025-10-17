using ChannelService.Domain.Enums;

namespace ChannelService.Application.Members
{
    /// <summary>
    /// Request to add a member to a channel
    /// </summary>
    public record AddMemberRequestDto(
        Guid UserId,
        MemberRole Role=MemberRole.Member);
}