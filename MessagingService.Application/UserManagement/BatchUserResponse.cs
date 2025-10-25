namespace MessagingService.Application.UserManagement
{
    public record BatchUserResponse(
        bool IsSuccess,
        Dictionary<Guid,string>? Data,
        string? Message=null);
}