using ChannelService.Domain.Enums;

namespace ChannelService.Application.Channels
{
    /// <summary>
    /// Complete channel information
    /// </summary>
    public record ChannelDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public ChannelType Type { get; init; }
        public bool IsArchived { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public int MemberCount { get; init; }
    }
}