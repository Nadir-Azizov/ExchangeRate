namespace BambooCard.WebService.Models;

public class ExchangeRateDto
{
    public decimal Amount { get; set; }
    public string Base { get; set; }
    public DateTimeOffset Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = [];
}
