using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Simplified channel information for list views.
    /// </summary>
    public record ChannelListDto(
        Guid Id,
        string Name,
        ChannelType Type,
        bool IsArchived,
        int MemberCount,
        DateTime CreatedAt);
}