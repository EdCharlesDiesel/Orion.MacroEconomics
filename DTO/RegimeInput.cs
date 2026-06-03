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
    public List<IndicatorResult> Indicators { get; set; } = new();
    public MarketRegime CurrentRegime { get; set; }
}

