namespace FileService.Application.DTOs
{
    public record FileAccessDto
    {
        public Guid Id { get; init; }
        public Guid FileId { get; init; }
        public Guid UserId { get; init; }
        public string UserDisplayName { get; set; } = string.Empty;
        public Guid GrantedBy { get; init;  }
        public string GrantedByDisplayName { get; set; } = string.Empty;
        public DateTime GrantedAt { get; init; }
        public bool IsRevoked { get; init;  }
        public DateTime? RevokedAt { get; init;  }
    }
}