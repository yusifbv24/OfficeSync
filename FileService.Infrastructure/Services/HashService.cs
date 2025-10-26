using FileService.Application.Interfaces;
using System.Security.Cryptography;

namespace FileService.Infrastructure.Services
{
    /// <summary>
    /// Implementation of hash service using SHA-256 cryptographic hash algorithm.
    /// 
    /// SHA-256 produces a 256-bit (32-byte) hash value, typically rendered as a
    /// 64-character hexadecimal string. This hash serves as a unique fingerprint
    /// of the file content - even the tiniest change to the file will produce
    /// a completely different hash.
    /// 
    /// The hash is computed by reading the file in chunks rather than loading
    /// it entirely into memory. This makes the operation efficient even for
    /// very large files (several GB), as memory usage remains constant regardless
    /// of file size.
    /// 
    /// Common uses for file hashing:
    /// 1. Integrity Verification: Detect if file has been corrupted or modified
    /// 2. Deduplication: Identify duplicate files to save storage space
    /// 3. Change Detection: Quickly determine if file content has changed
    /// </summary>
    public class HashService:IHashService
    {
        public async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Save original position so we can reset it after hasing
            var originalPosition=stream.Position;
            stream.Position = 0;

            try
            {
                // Create SHA-256 hash algoritm instance
                using var sha256=SHA256.Create();

                // Compute hash by reading stream in chunks
                // This is memory-efficient for large files
                var hashBytes=await sha256.ComputeHashAsync(stream, cancellationToken);

                // Convert byte array to hexadecimal string
                return BitConverter.ToString(hashBytes)
                    .Replace("-", "") // Remove hyphens from hex string
                    .ToLowerInvariant();  // Convert to lowercase for consistency
            }
            finally
            {
                // Reset stream position so it can be read again
                stream.Position = originalPosition;
            }
        }
    }
}