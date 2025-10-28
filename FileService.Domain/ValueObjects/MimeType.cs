using FileService.Domain.Enums;

namespace FileService.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a MIME type with validation and file type categorization.
    /// MIME types tell us what kind of file we are dealing with (image/jpeg, application/pdf, etc.).
    /// </summary>
    public class MimeType:IEquatable<MimeType>
    {
        public string Value { get; }
        private MimeType(string value)
        {
            Value= value;
        }

        /// <summary>
        /// Creates a MimeType value object with validation.
        /// </summary>
        public static MimeType Create(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be empty",nameof(contentType));

            if (!contentType.Contains('/'))
                throw new ArgumentException("Invalid MIME type format", nameof(contentType));

            var normalizedType = contentType.ToLowerInvariant().Trim();

            if(!IsAllowedMimeType(normalizedType))
                throw new ArgumentException($"File type '{contentType}' is not allowed",nameof(normalizedType));

            return new MimeType(normalizedType);
        }


        /// <summary>
        /// Determines the FileType category based on the MIME type.
        /// </summary>
        public FileType GetFileType()
        {
            if (Value.StartsWith("image/"))
                return FileType.Image;

            if(Value.StartsWith("video/"))
                return FileType.Video;

            if (Value.StartsWith("audio/"))
                return FileType.Video;

            if (Value.StartsWith("application/") &&
               (Value.Contains("zip") || Value.Contains("rar") || Value.Contains("tar") ||
                Value.Contains("7z") || Value.Contains("gzip")))
                return FileType.Archive;

            if (Value == "application/pdf" ||
                Value == "application/msword" ||
                Value == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                Value == "application/vnd.ms-excel" ||
                Value == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                Value == "application/vnd.ms-powerpoint" ||
                Value == "application/vnd.openxmlformats-officedocument.presentationml.presentation" ||
                Value.StartsWith("text/"))
                return FileType.Document;

            return FileType.Other;
        }


        /// <summary>
        /// Checks if a MIME type is allowed in the system.
        /// You can extend this list based on your security requirements.
        /// </summary>
        private static bool IsAllowedMimeType(string mimeType)
        {
            var allowedTypes = new HashSet<string>
            {
                // Images
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml",
            
                // Documents
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation", // .pptx
                "text/plain", "text/csv", "text/html", "text/xml",
            
                // Archives
                "application/zip", "application/x-zip-compressed",
                "application/x-rar-compressed", "application/x-7z-compressed",
                "application/x-tar", "application/gzip",
            
                // Videos
                "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
                "video/webm", "video/x-matroska",
            
                // Audio
                "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/webm"
            };

            return allowedTypes.Contains(mimeType);
        }


        public bool Equals(MimeType? other)
        {
            if (other is null) return false;
            return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }
        public override bool Equals(object? obj) => Equals(obj as MimeType);
        public override int GetHashCode()=>Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
        public override string ToString() => Value;
        public static implicit operator string(MimeType mimeType) => mimeType.Value;
    }
}