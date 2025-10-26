using FileService.Application.DTOs;
using FileService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Services
{
    /// <summary>
    /// Implementation of file storage service using the local file system.
    /// This service handles all physical file operations including saving, retrieving,
    /// and deleting files from disk storage.
    /// 
    /// The implementation creates a date-based directory structure (year/month/day)
    /// to prevent having too many files in a single directory, which can hurt
    /// filesystem performance. This structure also makes it easier to:
    /// - Archive old files by date
    /// - Identify when files were uploaded by looking at directory structure
    /// - Distribute I/O load across multiple directories
    /// 
    /// For your local network setup, files are stored on the server's local filesystem,
    /// but this abstraction means you could later move to network storage without
    /// changing the application code.
    /// </summary>
    public class FileStorageService:IFileStorageService
    {
        private readonly string _basePath;
        public FileStorageService(IOptions<FileStorageOptions> options)
        {
            _basePath=options.Value.BasePath;

            // Ensure the base storage directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<bool> DeleteFileAsync(
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            var fullPath=GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                // Delete the physical file
                File.Delete(fullPath);

                // Optionally clean up empty directories to keep storage tidy
                await CleanupEmptyDirectoriesAsync(Path.GetDirectoryName(fullPath));

                return true;
            }
            catch
            {
                // If deletion fails (file in use, permissions issue, etc.), return false
                return false;
            }
        }


        public async Task<bool> FileExistsAsync(
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            var fullPath=GetFullPath(filePath);
            return File.Exists(fullPath);
        }


        public async Task<Stream?> GetFileStreamAsync(
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            // Open file stream for reading
            // FileShare.Read allows other processes to read the file concurrently
            // This is important if multiple users download the same file simultaneously.
            return new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096, // 4KB buffer for efficient streaming
                useAsync: true); // Enable async I/O for better performance

        }


        public string GetFullPath(string relativePath)
        {
            // Convert relative path to full filesystem path
            return Path.Combine(_basePath, relativePath);
        }


        public async Task<string> SaveFileAsync(
            IFormFile file, 
            string fileName, 
            CancellationToken cancellationToken = default)
        {
            // Create date-based directory structure : uploads/2024/10/25
            var now=DateTime.UtcNow;
            var relativePath = Path.Combine(
                "uploads",
                now.Year.ToString(),
                now.Month.ToString("D2"), // D2 ensures two digits: 01,02
                now.Day.ToString("D2"),
                fileName);

            var fullPath= Path.Combine(_basePath, relativePath);

            // Ensure the directory exists
            var directory=Path.GetDirectoryName(fullPath);
            if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save the file to a disk
            // Using FileMode.Create overwrites it file exists(should not happen with Guid filenames)
            await using var fileStream=new FileStream(fullPath,FileMode.Create,FileAccess.Write);
            await file.CopyToAsync(fileStream, cancellationToken);

            // Return the relative path for database storage 
            // We store relative paths so we can change the base path later if needed
            return relativePath;
        }


        /// <summary>
        /// Helper method to remove empty directories after file deletion.
        /// This keeps the storage directory structure clean by removing
        /// directories that no longer contain any files.
        /// </summary>
        private async Task CleanupEmptyDirectoriesAsync(string? directoryPath)
        {
            if(string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            try
            {
                // Only delete if directory is empty
                if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
                {
                    Directory.Delete(directoryPath);

                    // Recursively clean up parent directories if they are also empty
                    var parentDirectory = Directory.GetParent(directoryPath)?.FullName;
                    if ((parentDirectory!=null && parentDirectory!=_basePath))
                    {
                        await CleanupEmptyDirectoriesAsync(parentDirectory);
                    }
                }
            }
            catch
            {
                // If cleanup fails, it's not critical - just leave the empty directories
            }
        }
    }
}