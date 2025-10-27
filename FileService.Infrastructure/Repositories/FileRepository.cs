using FileService.Application.Common;
using FileService.Application.Interfaces;
using FileService.Domain.Enums;
using FileService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using File = FileService.Domain.Entities.File;

namespace FileService.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of the File repository using Entity Framework Core.
    /// This class handles all database operations for files, implementing the
    /// IFileRepository interface that the application layer depends on.
    /// 
    /// The repository pattern provides several key benefits:
    /// 1. Abstracts EF Core complexity from business logic
    /// 2. Makes the code more testable by allowing repository mocking
    /// 3. Centralizes data access logic in one place
    /// 4. Provides a clean, domain-focused API for data operations
    /// 
    /// All queries use IQueryable for deferred execution, meaning the query
    /// isn't sent to the database until you actually enumerate the results.
    /// This allows EF Core to optimize the query and fetch only needed data.
    /// </summary>
    public class FileRepository:IFileRepository
    {
        private readonly FileServiceDbContext _context;

        public FileRepository(FileServiceDbContext context)
        {
            _context= context;
        }

        public async Task AddAsync(File file,CancellationToken cancellationToken)
        {
            await _context.Files.AddAsync(file,cancellationToken);
        }


        public async Task<File?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
        {
            return await _context.Files
                .Where(f=>f.FileHash== fileHash&& !f.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<File?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Only return non-deleted files for normal queries
            return await _context.Files
                .Where(f => f.Id == id && !f.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<File?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Used for restore operations - need to access soft-deleted records
            return await _context.Files
                .IgnoreQueryFilters() // This bypasses the global IsDeleted filter
                .Include(f => f.FileAccesses)
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<File?> GetByIdWithAccessesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Include FileAccess records for permission checking
            // This uses eager loading with Include() to fetch related data in a single query
            return await _context.Files
                .Include(f=>f.FileAccesses)
                .Where(f=>f.Id == id && !f.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }


        public async Task<PagedResult<File>> GetFilesAsync(
            Guid requesterId, 
            bool isAdmin, 
            Guid? channelId = null,
            Guid? uploadedBy = null,
            string? contentType = null,
            string? searchTerm = null,
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            int pageNumber = 1, 
            int pageSize = 20, 
            CancellationToken cancellationToken = default)
        {
            // Start with base query - only non-deleted files
            var query=_context.Files.Where(f=> !f.IsDeleted);

            // Apply permission filtering based on access levels
            if (!isAdmin)
            {
                // Non-admin users can see:
                // 1. Their own files (any access level)
                // 2. Public files
                // 3. Files where they have explicit access (for Restricted files)
                // Note: ChannelMembers filtering is handled at the application layer
                // because it requires a call to the Channel Service

                query = query.Where(f =>
                    f.UploadedBy == requesterId || // Own files
                    f.AccessLevel == FileAccessLevel.Public || // Public files
                    f.FileAccesses.Any(fa => fa.UserId == requesterId && !fa.IsRevoked)); // Explicit access
            }

            // Apply optional filters
            if (channelId.HasValue)
            {
                query=query.Where(f=>f.ChannelId == channelId.Value);
            }

            if (uploadedBy.HasValue)
            {
                query=query.Where(f=>f.UploadedBy == uploadedBy.Value);
            }

            if(!string.IsNullOrWhiteSpace(contentType))
            {
                // Support partial content type matching (e.g., "image/" matches all images)
                query=query.Where(f=>f.ContentType.StartsWith(contentType));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query=query.Where(f=>f.OriginalFileName.ToLower().Contains(lowerSearchTerm));
            }

            if (fromDate.HasValue)
            {
                query=query.Where(f=>f.UploadedAt>=fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(f => f.UploadedAt >= toDate.Value);
            }

            // Get total count before pagination
            // This count reflects all filters but not pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting - most recent files first
            query = query.OrderByDescending(f => f.UploadedAt);

            // Apply pagination
            // Skip calculates how many records to skip based on page number
            // Example : Page 2 with PageSize 20 skips the first 20 records
            var files = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<File>
            {
                Items = files,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }



        public async Task<List<File>> GetFilesByMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            // Get all files attached to a specific message
            // Used when displaying message attachments
            return await _context.Files
                .Where(f => f.MessageId == messageId && !f.IsDeleted)
                .OrderBy(f => f.UploadedAt)
                .ToListAsync(cancellationToken);
        }


        public async Task<long> GetTotalSizeByChannelAsync(Guid channelId, CancellationToken cancellationToken = default)
        {
            // Calculate total storage used in a channel
            var totalSize = await _context.Files
                .Where(f => f.ChannelId == channelId && !f.IsDeleted)
                .SumAsync(f => (long?)f.SizeInBytes, cancellationToken);
            return totalSize ?? 0;
        }

        public async Task<long> GetTotalSizeByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Calculate total storage used by a user
            // Sum is efficient in SQL -it's computed by the database, not in memory
            var totalSize = await _context.Files
                .Where(f => f.UploadedBy == userId && !f.IsDeleted)
                .SumAsync(f => (long?)f.SizeInBytes, cancellationToken);

            return totalSize ?? 0;
        }
    }
}