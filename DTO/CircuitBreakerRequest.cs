using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.DTO;

public class CircuitBreakerRequest
{
    public AccountContext Account { get; set; } = new();
    public List<TradePlan>? TodayTrades { get; set; }
    public List<TradePlan>? OpenTrades { get; set; }
    public DataQualityResult? DataQuality { get; set; }
}