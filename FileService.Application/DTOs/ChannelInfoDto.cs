namespace FileService.Application.DTOs
{
    public record ChannelInfoDto
    {
        public Guid ChannelId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}