namespace OrderManagementSystem.Application.DTOs.Common
{
    public class PaginatedResult<T>(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        public List<T> Items { get; } = items;
        public int PageNumber { get; } = pageNumber;
        public int PageSize { get; } = pageSize;
        public int TotalCount { get; } = totalCount;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
