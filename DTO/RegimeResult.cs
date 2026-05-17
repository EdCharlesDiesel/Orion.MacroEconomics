using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.DTO;

public sealed class RegimeResult
{
    /// <summary>The detected or held market regime.</summary>
    public MarketRegime Regime { get; set; }

    /// <summary>
    /// Normalised confidence in this regime (0–100).
    /// Derived from composite indicator scoring.
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Human-readable explanation of why this regime was selected,
    /// including key supporting signals and runner-up regime.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when this result was produced.</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Normalised score (0–100) per regime from the composite analysis.
    /// Null for single-indicator Detect() results.
    /// </summary>
    public IReadOnlyDictionary<MarketRegime, decimal>? ScoreBreakdown { get; set; }

    // ── Convenience accessors (derived — never stored separately) ──────────────

    /// <summary>Regime name as a string — use instead of a separate Name field.</summary>
    public string Name => Regime.ToString();

    /// <summary>Whether the result carries a strong signal (confidence ≥ 60).</summary>
    public bool IsHighConviction => Confidence >= 60m;

    /// <summary>Score for RiskOn regime from the breakdown, if available.</summary>
    public decimal RiskOnScore  => ScoreBreakdown?.GetValueOrDefault(MarketRegime.RiskOn)  ?? 0m;

    /// <summary>Score for RiskOff regime from the breakdown, if available.</summary>
    public decimal RiskOffScore => ScoreBreakdown?.GetValueOrDefault(MarketRegime.RiskOff) ?? 0m;
}