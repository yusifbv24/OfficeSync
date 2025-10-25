namespace UserManagementService.Application.DTOs.Users
{
    public record BatchUsersRequestDto(
        Guid[] UserIds);
}