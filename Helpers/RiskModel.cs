namespace Orion.MacroEconomics.Helpers
{
    public static class RiskModel
    {
        private const string Long  = "LONG";
        private const string Short = "SHORT";

        private const decimal DefaultSlMultiplier = 1.5m;
        private const decimal DefaultTpMultiplier = 3.0m;
        private const decimal FallbackVolatility  = 0.005m;
        private const decimal MinPrice            = 0.00001m;

        public static (decimal Sl, decimal Tp) Calculate(
            decimal close,
            decimal atr,
            string direction,
            decimal slMultiplier = DefaultSlMultiplier,
            decimal tpMultiplier = DefaultTpMultiplier)
        {
            if (close <= 0)
                throw new ArgumentOutOfRangeException(nameof(close), "Close must be positive.");

            if (atr <= 0)
                atr = close * FallbackVolatility;

            if (string.IsNullOrWhiteSpace(direction))
                throw new ArgumentException("Direction must be provided", nameof(direction));

            direction = direction.Trim().ToUpperInvariant();

            decimal stopLoss;
            decimal takeProfit;

            switch (direction)
            {
                case Long:
                    stopLoss   = close - slMultiplier * atr;
                    takeProfit = close + tpMultiplier * atr;
                    break;
                case Short:
                    stopLoss   = close + slMultiplier * atr;
                    takeProfit = close - tpMultiplier * atr;
                    break;
                default:
                    throw new ArgumentException($"Invalid direction: {direction}", nameof(direction));
            }

            stopLoss   = Math.Max(MinPrice, stopLoss);
            takeProfit = Math.Max(MinPrice, takeProfit);

            return (stopLoss, takeProfit);
        }

        public static decimal RiskRewardRatio(decimal entry, decimal stopLoss, decimal takeProfit)
        {
            var risk   = Math.Abs(entry - stopLoss);
            var reward = Math.Abs(takeProfit - entry);
            return risk == 0 ? 0 : reward / risk;
        }

        public static decimal PositionSize(decimal accountEquity, decimal riskPct, decimal entry, decimal stopLoss)
        {
            if (accountEquity <= 0 || riskPct <= 0)
                return 0;

            var perUnitRisk = Math.Abs(entry - stopLoss);
            if (perUnitRisk == 0)
                return 0;

            var riskAmount = accountEquity * riskPct;
            return riskAmount / perUnitRisk;
        }
    }
}
