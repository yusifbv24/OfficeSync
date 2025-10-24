namespace MessagingService.Application.UserManagement
{
    public record UserResponse(bool IsSuccess,UserData? Data);
}