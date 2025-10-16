namespace UserManagementService.Application.DTOs.Identity
{
    public record IdentityServiceResponse
    {
        public bool IsSuccess;
        public IdentityUserData? Data;
    }
}