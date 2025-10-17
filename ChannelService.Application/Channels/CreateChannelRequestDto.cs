using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Request to create a new channel
    /// </summary>
    public record CreateChannelRequestDto(
        string Name,
        string? Description,
        ChannelType Type);
}