namespace Template.Web.Core
{
    public class PaginatedList<T>
    {
        public PaginatedList(IEnumerable<T> content, long totalItems, long pageIndex, int pageSize)
        {
            Content = content;
            TotalItems = totalItems; PageIndex = pageIndex; PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        }

        public long TotalItems { get; init; }
        public long PageIndex { get; init; }
        public int PageSize { get; init; }
        public long TotalPages { get; init; }
        public IEnumerable<T> Content { get; init; }
    }
}