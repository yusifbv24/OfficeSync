namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Service interface for generating thumbnail images.
    /// Thumbnails improve UI performance by allowing clients to display small preview images
    /// instead of loading full-sized images. This is especially important for image galleries
    /// or file listings where many images are displayed at once.
    /// 
    /// Thumbnails are typically around 200x200 pixels, significantly smaller than originals,
    /// reducing bandwidth usage and improving page load times.
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Generates a thumbnail for an image file.
        /// Returns the relative path where the thumbnail was saved.
        /// 
        /// The thumbnail is stored alongside the original file with a naming convention
        /// that makes it easy to identify: thumb_{originalFileName}
        /// 
        /// If the file is not an image or thumbnail generation fails,
        /// returns null rather than throwing an exception. Thumbnail generation
        /// failures should not prevent file uploads from succeeding.
        /// </summary>
        Task<string?> GenerateThumbnailAsync(
            string originalFilePath,
            string originalFileName,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets the thumbnail stream for displaying.
        /// Similar to getting the file stream, but for thumbnails.
        /// </summary>
        Task<Stream?> GetThumbnailStreamAsync(
            string thumbnailPath,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Deletes a thumbnail file from storage.
        /// Called when the original file is permanently deleted.
        /// </summary>
        Task<bool> DeleteThumbnailAsync(
            string thumbnailPath,
            CancellationToken cancellationToken = default);
    }
}