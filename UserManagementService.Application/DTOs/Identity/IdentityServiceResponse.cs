namespace UserManagementService.Application.DTOs.Identity
{
    public record IdentityServiceResponse(bool IsSuccess,IdentityUserData? Data);
}