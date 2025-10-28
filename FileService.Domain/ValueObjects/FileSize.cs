namespace FileService.Domain.ValueObjects
{
    public class FileSize:IEquatable<FileSize>
    {
        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long Bytes { get; }

        /// <summary>
        /// Maximum allowed file size: 100 MB for general files.
        /// This can be configured differently for different file types.
        /// </summary>
        public const long MaxFileSizeBytes=100L*1024*1024; // 100 MB


        /// <summary>
        /// Maximum size for images: 10 MB.
        /// Images larger than this are probably too big for web usage.
        /// </summary>
        public const long MaxImageSizeBytes=10L*1024*1024; // 10 MB

        private FileSize(long bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        /// Creates a FileSize value object with validation.
        /// </summary>
        public static FileSize Create(long bytes,bool isImage = false)
        {
            if (bytes <= 0)
                throw new ArgumentException("File size must be greater than zero",nameof(bytes));

            var maxSize=isImage? MaxImageSizeBytes : MaxFileSizeBytes;

            if (bytes > maxSize)
            {
                var maxSizeMb = maxSize / (1024.0 * 1024.0);
                throw new ArgumentException(
                    $"File size {FormatBytes(bytes)} exceeds maximum allowed size of {maxSizeMb:F1} MB",
                    nameof(bytes));
            }
            return new FileSize(bytes);
        }

        public string ToHumanReadable()
        {
            return FormatBytes(Bytes);
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while(len>=1024 && order < sizes.Length - 1)
            {
                order++;
                len=len/1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public bool Equals(FileSize? other)
        {
            if (other is null) return false;
            return Bytes == other.Bytes;
        }

        public override bool Equals(object? obj)=>Equals(obj as FileSize);

        public override int GetHashCode()=>Bytes.GetHashCode();

        public override string ToString() => ToHumanReadable();
    }
}