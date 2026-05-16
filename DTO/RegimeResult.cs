using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.DTO;

public sealed class RegimeResult
{
    public string Name { get; set; } = "NEUTRAL";
    public MarketRegime CurrentRegime { get; set; }
    public MarketRegime Regime { get; set; }
    public decimal Confidence { get; set; }
    public string Reason { get; set; }
    public decimal Score { get; set; }
    public decimal RiskOnScore { get; set; }
    public decimal RiskOffScore { get; set; }
    public string Explanation { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyDictionary<MarketRegime, decimal>? ScoreBreakdown { get; set; }
}

