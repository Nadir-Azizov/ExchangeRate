using BambooCard.Domain.Entities.Base;
using BambooCard.Domain.Enums;

namespace BambooCard.Domain.Entities.Main;

public class Rate : BaseEntity
{
    public ECurrency Currency { get; set; }
    public decimal Value { get; set; }
    public int ExchangeRateId { get; set; }

    public ExchangeRate ExchangeRate { get; set; }
}