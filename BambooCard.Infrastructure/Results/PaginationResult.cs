namespace BambooCard.Infrastructure.Results;

public class PaginationResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public PaginationResult(IEnumerable<T> data, int totalRecords, int pageNumber, int pageSize)
    {
        Data = data;
        TotalRecords = totalRecords;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalRecords / (pageSize == 0 ? 1 : pageSize));
    }
}
