namespace FileService.Application.DTOs
{
    public record FileDto
    {
        public Guid Id { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public long SizeInBytes { get; init;  }
        public string FormattedSize => FormatFileSize(SizeInBytes);

        public Guid UploadedBy { get;init;  }
        public string UploaderDisplayName { get; set; }=string.Empty;
        public DateTime UploadedAt { get; init;  }

        public Guid? ChannelId { get; init;  }
        public Guid? MessageId { get; init; }
        public string AccessLevel { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int DownloadCount { get; init; }
        public bool? IsScanned { get; init; }
        public bool HasThumbnail { get; init; }
        public string DownloadUrl => $"/api/files/{Id}/download";
        public string? ThumbnailUrl => HasThumbnail ? $"/api/files/{Id}/thumbnail" : null;

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len=len/1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}