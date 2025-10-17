using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Request to update channel information.
    /// </summary>
    public record UpdateChannelRequestDto(
        string Name,
        string? Description,
        ChannelType Type);
}