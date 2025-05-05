using BambooCard.Domain.Enums;

namespace BambooCard.Business.Models.Main;

public record ExchangeRateDto
{
    public decimal Amount { get; set; }
    public ECurrency BaseCurrency { get; set; }
    public DateTimeOffset Date { get; set; }
    public Dictionary<ECurrency, decimal> Rates { get; set; }
}
