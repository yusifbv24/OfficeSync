namespace IdentityService.Domain.Entities
{
    public class RefreshToken:BaseEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public string CreatedByIp { get; set; }=string.Empty;

        public bool IsExpired=>DateTime.Now>=ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsActive;

        // Navigation property
        public User User { get; set; } = null!;
    }
}