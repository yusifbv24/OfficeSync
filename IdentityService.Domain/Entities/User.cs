namespace IdentityService.Domain.Entities
{
    public class User:BaseEntity
    {
        public string Username { get; set; }=string.Empty;
        public string Email { get;set;  }=string.Empty;
        public string PasswordHash { get; set; }=string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }

        // Navigation  properties
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}