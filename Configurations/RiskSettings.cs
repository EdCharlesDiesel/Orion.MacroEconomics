namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Risk management and indicator thresholds.
/// </summary>
public sealed class RiskSettings
{
    public decimal RiskPerTrade { get; set; } = 0.02m;

    public decimal AtrStopMultiplier { get; set; } = 2.0m;

    /// <summary>ATR multiplier used when sizing the stop-loss. Defaults to <see cref="AtrStopMultiplier"/>.</summary>
    public decimal AtrSlMultiplier { get; set; } = 2.0m;

    public decimal Tp1AtrMultiplier { get; set; } = 3.0m;

    public decimal Tp2AtrMultiplier { get; set; } = 5.0m;

    public decimal MinRiskReward { get; set; } = 2.0m;

    public decimal DefaultMinStop { get; set; } = 0.0010m;

    public decimal StopBufferPercent { get; set; } = 0.25m;

    public decimal AdxTrendMin { get; set; } = 20.0m;

    public decimal RsiOversold { get; set; } = 40.0m;

    public decimal RsiOverbought { get; set; } = 60.0m;

    public decimal StochOversold { get; set; } = 25.0m;

    public decimal StochOverbought { get; set; } = 75.0m;
}