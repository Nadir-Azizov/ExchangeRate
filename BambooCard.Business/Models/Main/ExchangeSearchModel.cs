using BambooCard.Domain.Enums;
using BambooCard.Business.Models.Pagination;

namespace BambooCard.Business.Models.Main;

public record ExchangeSearchModel : SearchModel
{
    public ECurrency? BaseCurrency { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
