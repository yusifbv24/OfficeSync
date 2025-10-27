namespace FileService.Application.DTOs
{
    public record LinkFileToMessageRequest(Guid MessageId, Guid ChannelId);
}