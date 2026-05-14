using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using RegimeResult = Orion.MacroEconomics.Entities.RegimeResult;

namespace Orion.MacroEconomics.Engine
{
    /// <summary>
    /// Detects market regime and simulates regime transitions.
    /// </summary>
    public sealed class RegimeEngine : IRegimeEngine
    {
        private readonly Random _random;
        private readonly Dictionary<MarketRegime, Dictionary<MarketRegime, decimal>> _transition;

        public RegimeEngine() : this(Random.Shared)
        {
        }

        internal RegimeEngine(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));

            _transition = new Dictionary<MarketRegime, Dictionary<MarketRegime, decimal>>
            {
                [MarketRegime.RiskOn] = new()
                {
                    [MarketRegime.RiskOn] = 0.70m,
                    [MarketRegime.RiskOff] = 0.20m,
                    [MarketRegime.Stagflation] = 0.10m
                },
                [MarketRegime.RiskOff] = new()
                {
                    [MarketRegime.RiskOff] = 0.60m,
                    [MarketRegime.RiskOn] = 0.20m,
                    [MarketRegime.Stagflation] = 0.20m
                },
                [MarketRegime.Stagflation] = new()
                {
                    [MarketRegime.Stagflation] = 0.55m,
                    [MarketRegime.RiskOff] = 0.30m,
                    [MarketRegime.RiskOn] = 0.15m
                }
            };
        }

        /// <inheritdoc />
        public MarketRegime Next(MarketRegime current)
        {
            if (!_transition.TryGetValue(current, out var probabilities))
                return current;

            var roll = (decimal)_random.NextDouble();
            var cumulative = 0m;

            foreach (var probability in probabilities)
            {
                cumulative += probability.Value;

                if (roll <= cumulative)
                    return probability.Key;
            }

            return current;
        }

        /// <inheritdoc />
        public RegimeResult Detect(NormalizedIndicator normalized)
        {
            ArgumentNullException.ThrowIfNull(normalized);

            var indicator = normalized.Indicator?.Trim().ToUpperInvariant() ?? string.Empty;
            var zScore = normalized.ZScore;
            var surprise = normalized.Surprise;
            var yoy = normalized.YoY;

            var regime = MarketRegime.RiskOn;
            var confidence = 50m;
            var reason = "Neutral macro conditions.";

            if (IsInflationIndicator(indicator) && zScore >= 1m && yoy > 0m)
            {
                regime = MarketRegime.Stagflation;
                confidence = 75m;
                reason = "Inflation pressure is above trend.";
            }
            else if (IsGrowthIndicator(indicator) && (zScore <= -1m || surprise < -0.05m))
            {
                regime = MarketRegime.RiskOff;
                confidence = 70m;
                reason = "Growth data is below trend or negatively surprised.";
            }
            else if (IsGrowthIndicator(indicator) && (zScore >= 1m || surprise > 0.05m))
            {
                regime = MarketRegime.RiskOn;
                confidence = 70m;
                reason = "Growth data is above trend or positively surprised.";
            }
            else if (zScore <= -1.5m)
            {
                regime = MarketRegime.RiskOff;
                confidence = 65m;
                reason = "Indicator is materially below trend.";
            }
            else if (zScore >= 1.5m)
            {
                regime = MarketRegime.RiskOn;
                confidence = 65m;
                reason = "Indicator is materially above trend.";
            }

            return new RegimeResult
            {
                Regime = regime,
                Confidence = confidence,
                Reason = reason,
                TimestampUtc = DateTime.UtcNow
            };
        }

        public object? Analyze(RegimeInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (input.Indicators is not { Count: > 0 })
                return new RegimeResult
                {
                    Regime       = Next(input.CurrentRegime),
                    Confidence   = 50m,
                    Reason       = "No indicators supplied — regime simulated from current state.",
                    TimestampUtc = DateTime.UtcNow,
                };

            // Detect regime per indicator, then aggregate by confidence-weighted vote
            var detections = input.Indicators.Select(Detect).ToList();

            var scores = detections
                .GroupBy(r => r.Regime)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Confidence));

            var (winningRegime, winningScore) = scores.MaxBy(kvp => kvp.Value);
            var totalScore      = scores.Values.Sum();
            var normalised      = totalScore > 0 ? Math.Round(winningScore / totalScore * 100m, 1) : 50m;

            var reasons = detections
                .Where(r => r.Regime == winningRegime)
                .Select(r => r.Reason)
                .Distinct();

            return new RegimeResult
            {
                Regime       = winningRegime,
                Confidence   = normalised,
                Reason       = string.Join(" ", reasons),
                TimestampUtc = DateTime.UtcNow,
            };
        }

        private static bool IsInflationIndicator(string indicator)
        {
            return indicator.Contains("CPI") ||
                   indicator.Contains("INFLATION") ||
                   indicator.Contains("PCE") ||
                   indicator.Contains("PPI");
        }

        private static bool IsGrowthIndicator(string indicator)
        {
            return indicator.Contains("GDP") ||
                   indicator.Contains("PMI") ||
                   indicator.Contains("PAYROLL") ||
                   indicator.Contains("EMPLOYMENT") ||
                   indicator.Contains("RETAIL");
        }
    }
}