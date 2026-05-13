using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Free
{
    public sealed class RegimeEngineFree : IRegimeEngineFree
    {
        public RegimeResult Analyze(RegimeInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var riskOnScore = 0m;
            var riskOffScore = 0m;
            var reasons = new List<string>();

            if (input.Vix < 15)
            {
                riskOnScore += 2m;
                reasons.Add("Low VIX supports risk-on.");
            }
            else if (input.Vix >= 25)
            {
                riskOffScore += 3m;
                reasons.Add("Elevated VIX supports risk-off.");
            }
            else if (input.Vix >= 20)
            {
                riskOffScore += 1.5m;
                reasons.Add("Moderate VIX pressure supports defensive positioning.");
            }

            if (input.GlobalEquityChangePercent > 1m)
            {
                riskOnScore += 2m;
                reasons.Add("Global equities are positive.");
            }
            else if (input.GlobalEquityChangePercent < -1m)
            {
                riskOffScore += 2m;
                reasons.Add("Global equities are under pressure.");
            }

            if (input.DollarIndexChangePercent > 0.75m)
            {
                riskOffScore += 1.5m;
                reasons.Add("Stronger USD suggests defensive demand.");
            }
            else if (input.DollarIndexChangePercent < -0.75m)
            {
                riskOnScore += 1m;
                reasons.Add("Weaker USD supports risk appetite.");
            }

            if (input.GlobalYieldChangeBps > 10m)
            {
                riskOffScore += 1m;
                reasons.Add("Sharp yield rise tightens financial conditions.");
            }
            else if (input.GlobalYieldChangeBps < -10m)
            {
                riskOnScore += 1m;
                reasons.Add("Falling yields ease financial conditions.");
            }

            if (input.InflationSurprise > 0.5m)
            {
                riskOffScore += 2m;
                reasons.Add("Positive inflation surprise increases policy risk.");
            }

            if (input.GrowthSurprise < -0.5m)
            {
                riskOffScore += 2m;
                reasons.Add("Negative growth surprise supports risk-off.");
            }
            else if (input.GrowthSurprise > 0.5m)
            {
                riskOnScore += 1.5m;
                reasons.Add("Positive growth surprise supports risk-on.");
            }

            if (input.PolicyRateSurprise > 0.25m)
            {
                riskOffScore += 2m;
                reasons.Add("Hawkish policy surprise supports risk-off.");
            }
            else if (input.PolicyRateSurprise < -0.25m)
            {
                riskOnScore += 1.5m;
                reasons.Add("Dovish policy surprise supports risk-on.");
            }

            var score = riskOnScore - riskOffScore;
            var total = Math.Abs(riskOnScore) + Math.Abs(riskOffScore);

            var regime = DetermineRegime(input, score);
            var confidence = total == 0
                ? 0m
                : Math.Min(100m, Math.Round(Math.Abs(score) / total * 100m, 2));

            return new RegimeResult
            {
                Regime = regime,
                Score = Math.Round(score, 2),
                RiskOnScore = Math.Round(riskOnScore, 2),
                RiskOffScore = Math.Round(riskOffScore, 2),
                Confidence = confidence,
                Explanation = reasons.Count == 0
                    ? "No strong regime signals detected."
                    : string.Join(" ", reasons),
                TimestampUtc = input.TimestampUtc
            };
        }

        private static MarketRegimeFree DetermineRegime(RegimeInput input, decimal score)
        {
            if (input.InflationSurprise > 1m && input.PolicyRateSurprise > 0.25m)
                return MarketRegimeFree.InflationShock;

            if (input.GrowthSurprise < -1m)
                return MarketRegimeFree.GrowthShock;

            if (Math.Abs(input.PolicyRateSurprise) > 0.5m)
                return MarketRegimeFree.PolicyShock;

            if (score >= 2m)
                return MarketRegimeFree.RiskOn;

            if (score <= -2m)
                return MarketRegimeFree.RiskOff;

            return MarketRegimeFree.Neutral;
        }
    }
}