namespace ChannelService.Application.Common
{
    /// <summary>
    /// Paginated result set for large datasets.
    /// </summary>
    public record PagedResult<T>
    {
        public List<T> Items { get; init; } = new();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResult<T> Create(List<T> items,int count,int pageNumber,int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = count,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages=(int)Math.Ceiling(count/(double)pageSize)
            };
        }
    }
}