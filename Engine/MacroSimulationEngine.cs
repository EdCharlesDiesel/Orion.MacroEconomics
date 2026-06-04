using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using Orion.MacroEconomics.Helpers.Interfaces;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Engine
{
    /// <summary>
    /// Stochastic macroeconomic forward simulator.
    /// Walks <see cref="MacroState"/> trajectories using a transition model and correlated shocks,
    /// and aggregates them into a <see cref="MacroSimulationResult"/>.
    /// </summary>
    public sealed class MacroSimulationEngine : IMacroSimulationEngine
    {
        private const int DefaultPaths = 200;

        private readonly IMacroTransitionModel _transition;
        private readonly ICorrelatedShockGenerator _shocks;

        public MacroSimulationEngine(
            IMacroTransitionModel transition,
            ICorrelatedShockGenerator shocks)
        {
            _transition = transition ?? throw new ArgumentNullException(nameof(transition));
            _shocks     = shocks     ?? throw new ArgumentNullException(nameof(shocks));
        }

        /// <inheritdoc />
        public List<MacroState> Run(MacroState initial, int steps)
        {
            ArgumentNullException.ThrowIfNull(initial);
            if (steps <= 0)
                throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be positive.");

            var states  = new List<MacroState>(steps + 1) { initial };
            var current = initial;
            var regime  = ClassifyRegime(initial);

            for (int i = 0; i < steps; i++)
            {
                var shock = _shocks.Generate();
                current   = _transition.Next(current, shock, regime);
                current.TimestampUtc = initial.TimestampUtc == default
                    ? DateTime.UtcNow
                    : initial.TimestampUtc.AddDays(i + 1);
                current.IsStable = IsStable(current);
                states.Add(current);

                regime = ClassifyRegime(current);
            }

            return states;
        }

        /// <inheritdoc />
        public MacroSimulationResult Simulate(
            NormalizedIndicator normalized,
            RegimeResult regime,
            ProbabilisticScenarioResult probabilities)
        {
            ArgumentNullException.ThrowIfNull(normalized);
            ArgumentNullException.ThrowIfNull(regime);
            ArgumentNullException.ThrowIfNull(probabilities);

            var seed   = SeedStateFrom(normalized, regime.Regime);
            var paths  = Math.Max(DefaultPaths / 4, probabilities.ScenarioCount);
            var horizon = 12; // monthly forward look

            int bullish = 0, bearish = 0, stable = 0;
            var aggregate = new List<MacroState>(paths * (horizon + 1));
            MarketRegime finalRegime = regime.Regime;

            for (int p = 0; p < paths; p++)
            {
                var path = Run(seed, horizon);
                aggregate.AddRange(path);

                var terminal = path[^1];
                if (terminal.IsStable) stable++;
                if (terminal.Growth > seed.Growth && terminal.RiskSentiment >= 0m) bullish++;
                else if (terminal.Growth < seed.Growth && terminal.RiskSentiment <= 0m) bearish++;

                finalRegime = ClassifyRegime(terminal);
            }

            var direction = bullish > bearish ? "LONG"
                          : bearish > bullish ? "SHORT"
                          : "NEUTRAL";

            decimal regimeWeight = Math.Clamp(regime.Confidence / 100m, 0m, 1m);
            decimal probabilityWeight = Math.Clamp(probabilities.Probability, 0m, 1m);
            decimal pathConviction = paths == 0
                ? 0m
                : (decimal)Math.Abs(bullish - bearish) / paths;

            var confidence = Math.Round(
                Math.Min(100m, 100m * pathConviction * (0.5m + 0.5m * regimeWeight) * (0.5m + 0.5m * probabilityWeight)),
                2);

            var successRate = paths == 0 ? 0m : Math.Round((decimal)stable / paths * 100m, 2);

            return new MacroSimulationResult
            {
                Direction    = direction,
                Confidence   = confidence,
                States       = aggregate,
                FinalRegime  = finalRegime,
                SuccessRate  = successRate,
                TimestampUtc = DateTime.UtcNow
            };
        }

        private static MacroState SeedStateFrom(NormalizedIndicator normalized, MarketRegime regime)
        {
            return new MacroState
            {
                Inflation        = IsInflation(normalized.Indicator) ? normalized.ZScore : normalized.InflationNormalized,
                InterestRate     = IsRate(normalized.Indicator)      ? normalized.ZScore : 0m,
                Growth           = IsGrowth(normalized.Indicator)    ? normalized.ZScore : normalized.GdpNormalized,
                GdpGrowth        = normalized.GdpNormalized,
                Sentiment        = normalized.SentimentNormalized,
                RiskSentiment    = regime switch
                {
                    MarketRegime.RiskOn      =>  0.25m,
                    MarketRegime.Goldilocks  =>  0.15m,
                    MarketRegime.RiskOff     => -0.25m,
                    MarketRegime.Stagflation => -0.20m,
                    _                        =>  0m
                },
                CurrencyStrength = string.IsNullOrWhiteSpace(normalized.Country)
                    ? new Dictionary<string, decimal>()
                    : new Dictionary<string, decimal>
                      {
                          [normalized.Country.Trim().ToUpperInvariant()] = normalized.ZScore
                      },
                TimestampUtc = normalized.Date == default ? DateTime.UtcNow : normalized.Date,
                IsStable     = true
            };
        }

        private static MarketRegime ClassifyRegime(MacroState state)
        {
            if (state.RiskSentiment >=  0.20m && state.Growth >= 0m) return MarketRegime.RiskOn;
            if (state.RiskSentiment <= -0.20m && state.Growth <= 0m) return MarketRegime.RiskOff;
            if (state.Inflation     >=  2m    && state.Growth <  0m) return MarketRegime.Stagflation;
            return MarketRegime.Goldilocks;
        }

        private static bool IsStable(MacroState state) =>
            Math.Abs(state.Inflation)    < 3m &&
            Math.Abs(state.InterestRate) < 5m &&
            Math.Abs(state.Growth)       < 4m;

        private static bool IsInflation(string indicator) =>
            indicator.Contains("CPI",       StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("INFLATION", StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("PCE",       StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("PPI",       StringComparison.OrdinalIgnoreCase);

        private static bool IsRate(string indicator) =>
            indicator.Contains("RATE",  StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("YIELD", StringComparison.OrdinalIgnoreCase);

        private static bool IsGrowth(string indicator) =>
            indicator.Contains("GDP",        StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("PMI",        StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("RETAIL",     StringComparison.OrdinalIgnoreCase) ||
            indicator.Contains("EMPLOYMENT", StringComparison.OrdinalIgnoreCase);
    }
}
