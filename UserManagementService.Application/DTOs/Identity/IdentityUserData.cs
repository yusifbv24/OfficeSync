namespace UserManagementService.Application.DTOs.Identity
{
    public record IdentityUserData
    {
        public Guid Id;
        public string Username;
        public string Email;
    }
}