namespace BambooCard.Business.Models.Pagination;

public record PaginationModel
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
