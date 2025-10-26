﻿namespace FileService.Application.Common
{
    public record UserProfileDto
    {
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; }=string.Empty;
        public string Role { get; init;  } = string.Empty;
        public string? AvatarUrl { get;init;  }
    }
}