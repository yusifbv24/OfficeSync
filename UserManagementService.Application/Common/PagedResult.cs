namespace UserManagementService.Application.Common
{
    /// <summary>
    /// Represents a paginated result set.
    /// Pagination is crucial for performance when dealing with large datasets.
    /// Instead of loading thousands of users at once, we load them in chunks.
    /// </summary>
    public record PagedResult<T>
    {
        public List<T> Items { get; init; } = [];
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Create a paginated result from a query.
        /// </summary>
        public static PagedResult<T> Create(List<T> items,int count, int pageNumber,int pageSize)
        {
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            return new PagedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = count,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };
        }   
    }
}