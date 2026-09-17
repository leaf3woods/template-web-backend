namespace Org.Product.Application.Dtos.Base;

public class PaginatedTimeQueryDto : QueryTimeDto
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
