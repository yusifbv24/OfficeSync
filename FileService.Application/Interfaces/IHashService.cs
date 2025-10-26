namespace FileService.Application.Interfaces
{
    /// <summary>
    /// Service interface for computing cryptographic hashes of files.
    /// File hashing serves two important purposes:
    /// 
    /// 1. Integrity Verification: The hash acts as a fingerprint of the file content.
    ///    You can later recompute the hash and compare it to detect corruption or tampering.
    /// 
    /// 2. Deduplication: Files with identical hashes are duplicates. You can detect
    ///    when users upload the same file multiple times and potentially reuse storage
    ///    instead of storing duplicates.
    /// 
    /// We use SHA-256, which provides a strong cryptographic hash that's extremely
    /// unlikely to have collisions (two different files producing the same hash).
    /// </summary>
    public interface IHashService
    {
        /// <summary>
        /// Computes the SHA-256 hash of a file stream.
        /// Returns the hash as a hexadecimal string.
        /// 
        /// The stream is read in chunks to avoid loading large files entirely into memory,
        /// making this method efficient even for very large files.
        /// 
        /// The stream position is reset to the beginning after computing the hash,
        /// so the same stream can be read again for other purposes.
        /// </summary>
        Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}