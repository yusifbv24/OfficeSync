namespace MessagingService.Application.Messages
{
    public record ForwardMessageRequestDto(
        Guid TargetChannelId,
        string? AdditionalComment = null);
}