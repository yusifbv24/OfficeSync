using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Complete channel information
    /// </summary>
    public record ChannelDto(
        Guid Id,
        string Name,
        string? Description,
        ChannelType Type,
        bool IsArchived,
        Guid CreatedBy,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int MemberCount);
}