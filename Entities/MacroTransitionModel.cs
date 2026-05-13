using Orion.MacroEconomics.Enum;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Entities
{
    public sealed class MacroTransitionModel : IMacroTransitionModel
    {
        public MacroState Next(
            MacroState prev,
            (decimal inf, decimal rate, decimal growth) shock,
            MarketRegime regime)
        {
            ArgumentNullException.ThrowIfNull(prev);

            var state = Clone(prev);

            state.Inflation += shock.inf;
            state.InterestRate += shock.rate;
            state.Growth += shock.growth;
            state.RiskSentiment = UpdateRisk(prev.RiskSentiment, regime);

            ApplyFeedback(state);

            return state;
        }

        public MacroState Next(
            MacroState current,
            ShockResult shocks,
            MarketRegime regime)
        {
            ArgumentNullException.ThrowIfNull(current);
            ArgumentNullException.ThrowIfNull(shocks);

            return Next(
                current,
                (
                    shocks.InflationShock,
                    shocks.RateShock,
                    shocks.GrowthShock
                ),
                regime);
        }

        public MacroState NextWithNormalization(
            NormalizedIndicator normalized,
            ShockResult shocks,
            MarketRegime regime)
        {
            ArgumentNullException.ThrowIfNull(normalized);
            ArgumentNullException.ThrowIfNull(shocks);

            var baseState = new MacroState
            {
                Inflation = IsInflation(normalized.Indicator) ? normalized.ZScore : 0m,
                InterestRate = IsRate(normalized.Indicator) ? normalized.ZScore : 0m,
                Growth = IsGrowth(normalized.Indicator) ? normalized.ZScore : 0m,
                RiskSentiment = regime == MarketRegime.RiskOn ? 0.25m :
                                regime == MarketRegime.RiskOff ? -0.25m :
                                0m,
                CurrencyStrength = new Dictionary<string, decimal>()
            };

            if (!string.IsNullOrWhiteSpace(normalized.Country))
                baseState.CurrencyStrength[normalized.Country.Trim().ToUpperInvariant()] = normalized.ZScore;

            return Next(baseState, shocks, regime);
        }

        private static MacroState Clone(MacroState state)
        {
            return new MacroState
            {
                Inflation = state.Inflation,
                InterestRate = state.InterestRate,
                Growth = state.Growth,
                RiskSentiment = state.RiskSentiment,
                CurrencyStrength = state.CurrencyStrength == null
                    ? new Dictionary<string, decimal>()
                    : new Dictionary<string, decimal>(state.CurrencyStrength)
            };
        }

        private static decimal UpdateRisk(decimal previous, MarketRegime regime)
        {
            return regime switch
            {
                MarketRegime.RiskOn => Math.Min(1m, previous + 0.1m),
                MarketRegime.RiskOff => Math.Max(-1m, previous - 0.1m),
                _ => previous
            };
        }

        private static void ApplyFeedback(MacroState state)
        {
            state.InterestRate += 0.5m * state.Inflation;
            state.Growth -= 0.3m * state.InterestRate;

            foreach (var currency in state.CurrencyStrength.Keys.ToList())
                state.CurrencyStrength[currency] += state.RiskSentiment * 0.05m;
        }

        private static bool IsInflation(string indicator)
        {
            return indicator.Contains("CPI", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("INFLATION", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("PCE", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("PPI", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRate(string indicator)
        {
            return indicator.Contains("RATE", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("YIELD", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGrowth(string indicator)
        {
            return indicator.Contains("GDP", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("PMI", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("RETAIL", StringComparison.OrdinalIgnoreCase)
                || indicator.Contains("EMPLOYMENT", StringComparison.OrdinalIgnoreCase);
        }
    }
}