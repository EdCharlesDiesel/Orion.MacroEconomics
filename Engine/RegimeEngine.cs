using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Engine;

/// <summary>
/// Detects market regime from normalised macro indicators and simulates
/// regime transitions via a calibrated first-order Markov chain.
/// </summary>
public sealed class RegimeEngine : IRegimeEngine
{
    private const decimal InflationZThreshold  = 1.0m;
    private const decimal GrowthZHigh          = 1.0m;
    private const decimal GrowthZLow           = -1.0m;
    private const decimal GrowthSurpriseHigh   = 0.05m;
    private const decimal GrowthSurpriseLow    = -0.05m;
    private const decimal GenericZHigh         = 1.5m;
    private const decimal GenericZLow          = -1.5m;
    private const decimal InflationConfidence  = 75m;
    private const decimal GrowthConfidence     = 70m;
    private const decimal GenericConfidence    = 65m;
    private const decimal NeutralConfidence    = 50m;

    
    private readonly Random _random;

    /// <summary>
    /// Calibrated transition matrix: P(next | current).
    /// Rows = current regime; inner dict = P(→ target regime).
    /// Row probabilities must sum to 1.0.
    /// </summary>
    private readonly IReadOnlyDictionary<MarketRegime, IReadOnlyDictionary<MarketRegime, decimal>> _transition;



    /// <summary>Creates a <see cref="RegimeEngine"/> backed by <see cref="Random.Shared"/>.</summary>
    public RegimeEngine() : this(Random.Shared) { }

    /// <summary>
    /// Creates a <see cref="RegimeEngine"/> with an explicit <see cref="Random"/> source.
    /// Useful for deterministic unit tests.
    /// </summary>
    internal RegimeEngine(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));

        _transition = new Dictionary<MarketRegime, IReadOnlyDictionary<MarketRegime, decimal>>
        {
            [MarketRegime.RiskOn] = new Dictionary<MarketRegime, decimal>
            {
                [MarketRegime.RiskOn]     = 0.70m,
                [MarketRegime.RiskOff]    = 0.20m,
                [MarketRegime.Stagflation] = 0.10m
            },
            [MarketRegime.RiskOff] = new Dictionary<MarketRegime, decimal>
            {
                [MarketRegime.RiskOff]    = 0.60m,
                [MarketRegime.RiskOn]     = 0.20m,
                [MarketRegime.Stagflation] = 0.20m
            },
            [MarketRegime.Stagflation] = new Dictionary<MarketRegime, decimal>
            {
                [MarketRegime.Stagflation] = 0.55m,
                [MarketRegime.RiskOff]    = 0.30m,
                [MarketRegime.RiskOn]     = 0.15m
            }
        };
    }



    /// <inheritdoc />
    public MarketRegime Next(MarketRegime current)
    {
        if (!_transition.TryGetValue(current, out var probabilities))
            return current;

        var roll       = (decimal)_random.NextDouble();
        var cumulative = 0m;

        foreach (var (regime, probability) in probabilities)
        {
            cumulative += probability;
            if (roll <= cumulative)
                return regime;
        }

        // Fallback — numerical guard against floating-point rounding.
        return current;
    }

    /// <inheritdoc />
    public RegimeResult Detect(NormalizedIndicator normalized)
    {
        ArgumentNullException.ThrowIfNull(normalized);

        var indicator = normalized.Indicator.Trim().ToUpperInvariant();
        var zScore    = normalized.ZScore;
        var surprise  = normalized.Surprise;
        var yoy       = normalized.YoY;

        // ── Rule priority: inflation > growth directional > generic z-score ──
        if (IsInflationIndicator(indicator) && zScore >= InflationZThreshold && yoy > 0m)
            return BuildResult(MarketRegime.Stagflation, InflationConfidence,
                "Inflation pressure is above trend with positive year-over-year momentum.");

        if (IsGrowthIndicator(indicator) && (zScore <= GrowthZLow || surprise < GrowthSurpriseLow))
            return BuildResult(MarketRegime.RiskOff, GrowthConfidence,
                "Growth data is below trend or negatively surprised.");

        if (IsGrowthIndicator(indicator) && (zScore >= GrowthZHigh || surprise > GrowthSurpriseHigh))
            return BuildResult(MarketRegime.RiskOn, GrowthConfidence,
                "Growth data is above trend or positively surprised.");

        if (zScore <= GenericZLow)
            return BuildResult(MarketRegime.RiskOff, GenericConfidence,
                "Indicator is materially below its long-run trend.");

        if (zScore >= GenericZHigh)
            return BuildResult(MarketRegime.RiskOn, GenericConfidence,
                "Indicator is materially above its long-run trend.");

        return BuildResult(MarketRegime.RiskOn, NeutralConfidence,
            "Neutral macro conditions — no strong directional signal detected.");
    }

    /// <inheritdoc />
    public RegimeResult Analyze(RegimeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Indicators is not { Count: > 0 })
            return BuildResult(
                Next(input.CurrentRegime),
                NeutralConfidence,
                "No indicators supplied — regime simulated from current state via Markov transition.");

        var detections = input.Indicators
            .Select(x=>x.)
            .ToList();

        var scores = detections
            .GroupBy(r => r.Regime)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Confidence));

        var (winningRegime, winningScore) = scores.MaxBy(kvp => kvp.Value);
        var totalScore  = scores.Values.Sum();
        var normalised  = totalScore > 0
            ? Math.Round(winningScore / totalScore * 100m, 1)
            : NeutralConfidence;

        var reason = string.Join(" ", detections
            .Where(r => r.Regime == winningRegime)
            .Select(r => r.Reason)
            .Distinct());

        // Cast to IReadOnlyDictionary<MarketRegime, decimal> for the record.
        IReadOnlyDictionary<MarketRegime, decimal> breakdown = scores;

        return BuildResult(winningRegime, normalised, reason, breakdown);
    }


    private static RegimeResult BuildResult(
        MarketRegime regime,
        decimal confidence,
        string reason,
        IReadOnlyDictionary<MarketRegime, decimal>? scoreBreakdown = null)
    {
        return new RegimeResult
        {
            Regime         = regime,
            Confidence     = confidence,
            Reason         = reason,
            TimestampUtc   = DateTime.UtcNow,
            ScoreBreakdown = scoreBreakdown
        };
    }

    private static bool IsInflationIndicator(string indicator) =>
        indicator.Contains("CPI", StringComparison.Ordinal)       ||
        indicator.Contains("INFLATION", StringComparison.Ordinal) ||
        indicator.Contains("PCE", StringComparison.Ordinal)       ||
        indicator.Contains("PPI", StringComparison.Ordinal);

    private static bool IsGrowthIndicator(string indicator) =>
        indicator.Contains("GDP", StringComparison.Ordinal)        ||
        indicator.Contains("PMI", StringComparison.Ordinal)        ||
        indicator.Contains("PAYROLL", StringComparison.Ordinal)    ||
        indicator.Contains("EMPLOYMENT", StringComparison.Ordinal) ||
        indicator.Contains("RETAIL", StringComparison.Ordinal);
}