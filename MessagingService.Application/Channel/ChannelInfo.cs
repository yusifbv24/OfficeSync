namespace MessagingService.Application.Channel
{
    public record ChannelInfo(
        Guid Id,
        string Name,
        bool IsArchived);
}