using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.DTO;

public class RegimeInput
{
    public decimal Vix { get; set; }
    public decimal DollarIndexChangePercent { get; set; }
    public decimal GlobalEquityChangePercent { get; set; }
    public decimal GlobalYieldChangeBps { get; set; }
    public decimal InflationSurprise { get; set; }
    public decimal GrowthSurprise { get; set; }
    public decimal PolicyRateSurprise { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public object Indicators { get; set; }
    public MarketRegime CurrentRegime { get; set; }
}

public sealed class RegimeResult
{
    public MarketRegimeFree Regime { get; set; }
    public decimal Score { get; set; }
    public decimal RiskOnScore { get; set; }
    public decimal RiskOffScore { get; set; }
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}