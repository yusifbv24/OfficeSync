namespace FileService.Application.DTOs.Files
{
    public record UploadFileRequestDto(
        Guid? ChannelId,
        Guid? ConversationId,
        string? Description);
}