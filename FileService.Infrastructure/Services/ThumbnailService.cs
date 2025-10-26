using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FileService.Infrastructure.Services
{
    /// <summary>
    /// Implementation of thumbnail generation service using ImageSharp library.
    /// ImageSharp is a modern, cross-platform image processing library that provides
    /// excellent performance and quality for image manipulation tasks.
    /// 
    /// Thumbnails are generated at a fixed size (200x200 by default) with aspect ratio
    /// preserved. The thumbnail is scaled to fit within these dimensions, maintaining
    /// the original image's proportions. This prevents distortion while ensuring
    /// consistent thumbnail sizes across the application.
    /// 
    /// Thumbnails are stored alongside original files with a "thumb_" prefix,
    /// making them easy to identify and manage.
    /// </summary>
    public class ThumbnailService:IThumbnailService
    {
        private readonly string _basePath;
        private readonly int _thumbnailWidth;
        private readonly int _thumbnailHeight;

        public ThumbnailService(IOptions<FileStorageOptions> options)
        {
            _basePath= options.Value.BasePath;
            _thumbnailWidth = 200;
            _thumbnailHeight = 200;
        }

        public async Task<string?> GenerateThumbnailAsync(
            string originalFilePath,
            string originalFileName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var fullOriginalPath=Path.Combine(_basePath,originalFilePath);
                if (!File.Exists(fullOriginalPath))
                {
                    return null;
                }

                // Load the image from disk
                using var image = await Image.LoadAsync(fullOriginalPath, cancellationToken);

                // Calculate thumbnail size while preserving aspect ratio
                // ResizeMode.Max ensures the image fits within the bounds without distortion
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_thumbnailHeight, _thumbnailWidth),
                    Mode = ResizeMode.Max // Fit within bounds, maintain aspect ratio
                }));

                // Generate thumbnail path in the same directory as original
                var thumbnailFileName = $"thumb_{originalFileName}";
                var thumbnailRelativePath = Path.Combine(
                    Path.GetDirectoryName(originalFileName) ?? string.Empty, thumbnailFileName);

                var fullThumbnailPath = Path.Combine(_basePath, thumbnailRelativePath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullThumbnailPath);
                if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save the thumbnail
                // ImageSharp automatically detects the format from the file extension
                await image.SaveAsync(fullThumbnailPath, cancellationToken);

                return thumbnailRelativePath;
            }
            catch
            {
                // Thumbnail generation is not critical - if it fails, just return null
                // The file upload should still succeed even without a thumbnail
                return null;
            }
        }


        public async Task<Stream?> GetThumbnailStreamAsync(
            string thumbnailPath,
            CancellationToken cancellationToken = default)
        {
            var fullPath=Path.Combine(_basePath,thumbnailPath);
            if(!File.Exists(fullPath))
            {
                return null;
            }
            return new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
        }


        public async Task<bool> DeleteThumbnailAsync(
            string thumbnailPath,
            CancellationToken cancellationToken = default)
        {
            var fullPath=Path.Combine(_basePath,thumbnailPath);
            if (!File.Exists(fullPath))
            {
                return false; 
            }

            try
            {
                File.Delete(fullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}