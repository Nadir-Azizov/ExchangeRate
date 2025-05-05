namespace BambooCard.Business.Models.Pagination;

public record SearchModel : PaginationModel
{
    public string Search { get; set; }
}