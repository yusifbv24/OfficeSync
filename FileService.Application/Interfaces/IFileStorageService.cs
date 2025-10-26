using Microsoft.AspNetCore.Http;

namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Service interface for physical file storage operations.
    /// This abstracts the underlying storage mechanism (local filesystem, cloud storage, etc.),
    /// allowing you to swap storage implementations without changing business logic.
    /// 
    /// For your local network setup, this will be implemented using the local file system,
    /// but the abstraction means you could later move to network storage or cloud
    /// storage without changing application code.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to physical storage.
        /// Returns the relative path where the file was saved.
        /// 
        /// The implementation creates a directory structure based on date (year/month/day)
        /// to prevent too many files in a single directory, which can hurt performance.
        /// 
        /// Format: uploads/{year}/{month}/{day}/{filename}
        /// Example: uploads/2024/10/25/a1b2c3d4_document.pdf
        /// </summary>
        Task<string> SaveFileAsync(
            IFormFile file,
            string fileName,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Opens a file stream for reading the file content.
        /// This is used when downloading files - the stream is sent directly
        /// to the HTTP response to avoid loading large files into memory.
        /// 
        /// The caller is responsible for disposing the stream after use.
        /// </summary>
        Task<Stream?> GetFileStreamAsync(
            string filePath,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Deletes the physical file from storage.
        /// This is called when permanently deleting files (not soft delete).
        /// Returns true if deletion was successful, false otherwise.
        /// </summary>
        Task<bool> DeleteFileAsync(
            string filePath,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Checks if a file exists at the specified path.
        /// Useful for validating file integrity before operations.
        /// </summary>
        Task<bool> FileExistsAsync(
            string filePath,
            CancellationToken cancellationToken = default);



        /// <summary>
        /// Gets the full physical path for a relative file path.
        /// Used internally to convert database paths to actual filesystem paths.
        /// </summary>
        string GetFullPath(string relativePath);
    }
}