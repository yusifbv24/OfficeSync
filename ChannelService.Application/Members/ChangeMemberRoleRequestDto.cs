using ChannelService.Domain.Enums;

namespace ChannelService.Application.Members
{
    /// <summary>
    /// Request to change a member's role
    /// </summary>
    public record ChangeMemberRoleRequestDto(
        MemberRole Role);
}