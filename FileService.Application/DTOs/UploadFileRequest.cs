using FileService.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FileService.Application.DTOs
{
    public record UploadFileRequest
    {
        public IFormFile File { get; set; } = null!;
        public Guid? ChannelId { get; set;  }
        public Guid? MessageId { get; set; }
        public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Private;
        public string? Description { get; set;  }
    }
}