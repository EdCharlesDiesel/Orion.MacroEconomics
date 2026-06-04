namespace Orion.MacroEconomics.Helpers
{
    public static class PerformanceMetrics
    {
        private const decimal TradingDaysPerYear = 252m;

        public static decimal SharpeRatio(List<decimal> returns, decimal riskFreeRate = 0m)
        {
            if (returns == null || returns.Count == 0)
                return 0;

            var excess = returns.Select(r => r - riskFreeRate / TradingDaysPerYear).ToList();
            var avg    = excess.Average();
            var std    = StdDev(excess);

            return std == 0 ? 0 : avg / std * DecimalMath.Sqrt(TradingDaysPerYear);
        }

        public static decimal SortinoRatio(List<decimal> returns, decimal riskFreeRate = 0m)
        {
            if (returns == null || returns.Count == 0)
                return 0;

            var perPeriodRfr = riskFreeRate / TradingDaysPerYear;
            var excess       = returns.Select(r => r - perPeriodRfr).ToList();
            var downside     = excess.Where(r => r < 0).ToList();
            if (downside.Count == 0)
                return 0;

            var downStd = DecimalMath.Sqrt(downside.Sum(r => r * r) / downside.Count);
            if (downStd == 0)
                return 0;

            return excess.Average() / downStd * DecimalMath.Sqrt(TradingDaysPerYear);
        }

        public static decimal MaxDrawdown(List<decimal> equity)
        {
            if (equity == null || equity.Count == 0)
                return 0;

            decimal peak  = equity[0];
            decimal maxDd = 0;

            foreach (var e in equity)
            {
                if (e > peak) peak = e;

                if (peak == 0) continue;
                var dd = (peak - e) / peak;
                if (dd > maxDd) maxDd = dd;
            }

            return maxDd;
        }

        public static decimal CalmarRatio(List<decimal> returns, List<decimal> equity)
        {
            var mdd = MaxDrawdown(equity);
            if (mdd == 0 || returns == null || returns.Count == 0)
                return 0;

            var annualReturn = returns.Average() * TradingDaysPerYear;
            return annualReturn / mdd;
        }

        public static decimal Volatility(List<decimal> returns) =>
            returns == null || returns.Count == 0
                ? 0
                : StdDev(returns) * DecimalMath.Sqrt(TradingDaysPerYear);

        private static decimal StdDev(IReadOnlyCollection<decimal> values)
        {
            if (values.Count == 0)
                return 0;
            var avg = values.Average();
            return DecimalMath.Sqrt(values.Sum(r => (r - avg) * (r - avg)) / values.Count);
        }
    }
}
