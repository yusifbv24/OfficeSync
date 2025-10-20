using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Simplified channel information for list views.
    /// </summary>
    public record ChannelListDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public ChannelType Type { get; init; }
        public bool IsArchived { get; init; }
        public int MemberCount { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}