using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Engine;

/// <summary>
/// Production regime engine: pure data-driven, deterministic multi-factor scoring.
/// No randomness. Regime is derived entirely from incoming indicator signals.
/// Supports hysteresis to prevent rapid regime flipping.
/// </summary>
public sealed class RegimeEngine : IRegimeEngine
{
    // ── Hysteresis ─────────────────────────────────────────────────────────────
    // A regime change only fires when the challenger's score leads the
    // current regime's score by at least this margin (0–100 scale).
    private const decimal HysteresisMargin = 10m;

    // ── Z-score thresholds ─────────────────────────────────────────────────────
    private const decimal StrongSignal  = 1.5m;
    private const decimal ModestSignal  = 0.75m;

    // ── Surprise thresholds ────────────────────────────────────────────────────
    private const decimal StrongSurprise = 0.05m;
    private const decimal ModestSurprise = 0.02m;

    // ── Per-rule confidence weights (sum drives composite score) ───────────────
    private const decimal WeightStrong = 30m;
    private const decimal WeightModest = 15m;
    private const decimal WeightWeak   =  8m;

    // ── Constructor ────────────────────────────────────────────────────────────
    // No dependencies — fully self-contained, stateless per invocation.
    public RegimeEngine() { }

    // ──────────────────────────────────────────────────────────────────────────
    // IRegimeEngine.Detect  — classify a single normalised indicator
    // ──────────────────────────────────────────────────────────────────────────
    public RegimeResult Detect(NormalizedIndicator normalized)
    {
        ArgumentNullException.ThrowIfNull(normalized);

        var tag      = normalized.Indicator.Trim().ToUpperInvariant();
        var z        = normalized.ZScore;
        var surprise = normalized.Surprise;
        var yoy      = normalized.YoY;

        return ClassifyIndicator(tag, z, surprise, yoy);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IRegimeEngine.Analyze — score multiple indicators, apply hysteresis
    // ──────────────────────────────────────────────────────────────────────────
    public RegimeResult Analyze(RegimeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Indicators is not { Count: > 0 })
        {
            // No data — hold current regime with low conviction.
            return BuildResult(
                input.CurrentRegime,
                30m,
                "No indicators supplied — holding current regime with low conviction.");
        }

        // Score every indicator against every regime.
        var detections = input.Indicators
            .Select(i => ClassifyIndicator(
                i.Indicator.Trim().ToUpperInvariant(),
                i.ZScore,
                i.Surprise,
                i.YoY))
            .ToList();

        // Aggregate raw scores per regime.
        var scores = new Dictionary<MarketRegime, decimal>
        {
            [MarketRegime.RiskOn]      = 0m,
            [MarketRegime.RiskOff]     = 0m,
            [MarketRegime.Stagflation] = 0m,
        };

        foreach (var d in detections)
            scores[d.Regime] += d.Confidence;

        var totalScore = scores.Values.Sum();
        if (totalScore == 0m)
        {
            return BuildResult(
                input.CurrentRegime,
                30m,
                "Indicators produced no directional signal — holding current regime.");
        }

        // Normalise to 0-100.
        var normalised = scores.ToDictionary(
            kvp => kvp.Key,
            kvp => Math.Round(kvp.Value / totalScore * 100m, 1));

        var challengerRegime     = normalised.MaxBy(kvp => kvp.Value).Key;
        var challengerConfidence = normalised[challengerRegime];
        var currentConfidence    = normalised.GetValueOrDefault(input.CurrentRegime, 0m);

        // Apply hysteresis: only switch if challenger leads by enough.
        var winningRegime = (challengerRegime != input.CurrentRegime
            && challengerConfidence - currentConfidence >= HysteresisMargin)
            ? challengerRegime
            : input.CurrentRegime;

        var winningConfidence = normalised[winningRegime];

        // Build a human-readable reason from all detections that support winner.
        var reason = BuildReason(detections, winningRegime, winningConfidence,
            challengerRegime, challengerConfidence, input.CurrentRegime);

        IReadOnlyDictionary<MarketRegime, decimal> breakdown = normalised;

        return BuildResult(winningRegime, winningConfidence, reason, breakdown);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IRegimeEngine.Next — data-driven forward projection
    // Returns the regime most likely to follow given the current one and a
    // set of leading indicator signals. No random element.
    // ──────────────────────────────────────────────────────────────────────────
    public RegimeResult Next(MarketRegime current)
    {
        // Without indicator data we express the base-rate persistence:
        // regimes are sticky — the most likely next regime is the current one.
        // Callers that have forward-looking indicators should use Analyze() instead.
        return BuildResult(
            current,
            60m,
            "Regime persistence assumed — supply leading indicators to Analyze() for a data-driven projection.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Core classification — deterministic, rule-based, weighted scoring
    // ──────────────────────────────────────────────────────────────────────────
    private static RegimeResult ClassifyIndicator(
        string tag, decimal z, decimal surprise, decimal yoy)
    {
        // ── Inflation indicators ───────────────────────────────────────────────
        if (IsInflation(tag))
        {
            if (z >= StrongSignal && yoy > 0m)
                return BuildResult(MarketRegime.Stagflation, WeightStrong,
                    $"{tag}: strong inflation Z={z:F2}, YoY positive — stagflation signal.");

            if (z >= ModestSignal)
                return BuildResult(MarketRegime.Stagflation, WeightModest,
                    $"{tag}: moderate inflation pressure Z={z:F2}.");

            if (z <= -ModestSignal)
                return BuildResult(MarketRegime.RiskOn, WeightModest,
                    $"{tag}: falling inflation Z={z:F2} — supportive of risk assets.");
        }

        // ── Growth indicators ──────────────────────────────────────────────────
        if (IsGrowth(tag))
        {
            if (z >= StrongSignal || surprise > StrongSurprise)
                return BuildResult(MarketRegime.RiskOn, WeightStrong,
                    $"{tag}: strong growth Z={z:F2}, surprise={surprise:P1} — risk-on.");

            if (z >= ModestSignal || surprise > ModestSurprise)
                return BuildResult(MarketRegime.RiskOn, WeightModest,
                    $"{tag}: modest growth beat Z={z:F2}.");

            if (z <= -StrongSignal || surprise < -StrongSurprise)
                return BuildResult(MarketRegime.RiskOff, WeightStrong,
                    $"{tag}: growth deteriorating Z={z:F2}, surprise={surprise:P1} — risk-off.");

            if (z <= -ModestSignal || surprise < -ModestSurprise)
                return BuildResult(MarketRegime.RiskOff, WeightModest,
                    $"{tag}: modest growth miss Z={z:F2}.");
        }

        // ── Labour market indicators ───────────────────────────────────────────
        if (IsLabour(tag))
        {
            // Rising unemployment = risk-off; falling = risk-on.
            if (z >= StrongSignal)
                return BuildResult(MarketRegime.RiskOff, WeightStrong,
                    $"{tag}: unemployment rising sharply Z={z:F2} — risk-off.");

            if (z >= ModestSignal)
                return BuildResult(MarketRegime.RiskOff, WeightWeak,
                    $"{tag}: labour market softening Z={z:F2}.");

            if (z <= -ModestSignal)
                return BuildResult(MarketRegime.RiskOn, WeightModest,
                    $"{tag}: tight labour market Z={z:F2} — supportive.");
        }

        // ── Credit / spread indicators ─────────────────────────────────────────
        if (IsCredit(tag))
        {
            if (z >= StrongSignal)
                return BuildResult(MarketRegime.RiskOff, WeightStrong,
                    $"{tag}: credit stress Z={z:F2} — risk-off.");

            if (z >= ModestSignal)
                return BuildResult(MarketRegime.RiskOff, WeightModest,
                    $"{tag}: widening spreads Z={z:F2}.");

            if (z <= -ModestSignal)
                return BuildResult(MarketRegime.RiskOn, WeightModest,
                    $"{tag}: tight credit conditions Z={z:F2} — risk-on.");
        }

        // ── Monetary policy / rates indicators ─────────────────────────────────
        if (IsRates(tag))
        {
            // Rising real rates with high inflation = stagflation pressure.
            if (z >= StrongSignal && yoy > 0m)
                return BuildResult(MarketRegime.Stagflation, WeightModest,
                    $"{tag}: rising rates with positive YoY — stagflation watch.");

            if (z >= ModestSignal)
                return BuildResult(MarketRegime.RiskOff, WeightWeak,
                    $"{tag}: tightening financial conditions Z={z:F2}.");

            if (z <= -ModestSignal)
                return BuildResult(MarketRegime.RiskOn, WeightWeak,
                    $"{tag}: easing financial conditions Z={z:F2}.");
        }

        // ── Generic fallback — weak signal from z-score alone ──────────────────
        if (z >= StrongSignal)
            return BuildResult(MarketRegime.RiskOn, WeightWeak,
                $"{tag}: above-trend Z={z:F2} — mild risk-on tilt.");

        if (z <= -StrongSignal)
            return BuildResult(MarketRegime.RiskOff, WeightWeak,
                $"{tag}: below-trend Z={z:F2} — mild risk-off tilt.");

        // Truly neutral — no signal.
        return BuildResult(MarketRegime.RiskOn, 0m,
            $"{tag}: neutral — Z={z:F2} within noise band.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private static string BuildReason(
        IReadOnlyList<RegimeResult> detections,
        MarketRegime winner,
        decimal winnerConfidence,
        MarketRegime challenger,
        decimal challengerConfidence,
        MarketRegime prior)
    {
        var supporting = detections
            .Where(d => d.Regime == winner && !string.IsNullOrWhiteSpace(d.Reason))
            .Select(d => d.Reason!)
            .Distinct()
            .Take(3)
            .ToList();

        var regime = winner == prior ? "Holding" : "Switching to";
        var summary = $"{regime} {winner} ({winnerConfidence:F1}% composite score).";

        if (winner != challenger)
            summary += $" Runner-up: {challenger} at {challengerConfidence:F1}%.";

        if (supporting.Any())
            summary += " Key signals: " + string.Join(" ", supporting);

        return summary;
    }

    private static RegimeResult BuildResult(
        MarketRegime regime,
        decimal confidence,
        string reason,
        IReadOnlyDictionary<MarketRegime, decimal>? breakdown = null) =>
        new()
        {
            Regime         = regime,
            Confidence     = Math.Clamp(confidence, 0m, 100m),
            Reason         = reason,
            TimestampUtc   = DateTime.UtcNow,
            ScoreBreakdown = breakdown
        };

    // ── Indicator family matchers ──────────────────────────────────────────────
    private static bool IsInflation(string t) =>
        t.Contains("CPI")       || t.Contains("INFLATION") ||
        t.Contains("PCE")       || t.Contains("PPI")       ||
        t.Contains("BREAKEVEN") || t.Contains("TIPS");

    private static bool IsGrowth(string t) =>
        t.Contains("GDP")        || t.Contains("PMI")      ||
        t.Contains("ISM")        || t.Contains("RETAIL")   ||
        t.Contains("INDUSTRIAL") || t.Contains("OUTPUT");

    private static bool IsLabour(string t) =>
        t.Contains("PAYROLL")    || t.Contains("EMPLOYMENT") ||
        t.Contains("UNEMPLOYMENT")|| t.Contains("JOLTS")     ||
        t.Contains("CLAIMS");

    private static bool IsCredit(string t) =>
        t.Contains("SPREAD")   || t.Contains("HY")   ||
        t.Contains("IG")       || t.Contains("OAS")  ||
        t.Contains("CDS")      || t.Contains("LIBOR");

    private static bool IsRates(string t) =>
        t.Contains("RATE")     || t.Contains("YIELD")  ||
        t.Contains("FOMC")     || t.Contains("FED")    ||
        t.Contains("SOFR")     || t.Contains("EURIBOR");
}