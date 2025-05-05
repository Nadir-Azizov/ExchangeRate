using BambooCard.Domain.Entities.Base;
using BambooCard.Domain.Enums;
using BambooCard.Infrastructure.Enums;

namespace BambooCard.Domain.Entities.Main;

public class ExchangeRate : BaseEntity
{
    public ECurrency BaseCurrency { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset Date { get; set; }
    public EProvider Provider { get; set; }

    public List<Rate> Rates { get; set; }
}
