using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers
{
    public static class ProbabilisticAggregator
    {
        private const decimal DefaultConfidenceLevel = 0.95m;

        public static ProbabilisticResult Aggregate(
            List<SimulationResult> sims,
            decimal confidenceLevel = DefaultConfidenceLevel)
        {
            if (sims == null || sims.Count == 0)
                return new ProbabilisticResult();

            if (confidenceLevel <= 0m || confidenceLevel >= 1m)
                throw new ArgumentOutOfRangeException(nameof(confidenceLevel));

            var returns = sims.Select(x => x.PortfolioReturn).ToList();

            var mean   = returns.Average();
            var std    = DecimalMath.Sqrt(returns.Sum(r => (r - mean) * (r - mean)) / returns.Count);
            var sorted = returns.OrderBy(x => x).ToList();

            var tailFraction = 1m - confidenceLevel;
            var tailCount    = Math.Max(1, (int)(tailFraction * sorted.Count));

            var varAtConfidence = sorted[tailCount - 1];
            var expectedShortfall = sorted.Take(tailCount).Average();

            var probLoss = (decimal)returns.Count(r => r < 0) / returns.Count;

            return new ProbabilisticResult
            {
                MeanReturn        = mean,
                StdDev            = std,
                ValueAtRisk95     = varAtConfidence,
                ExpectedShortfall = expectedShortfall,
                ProbabilityOfLoss = probLoss,
                Distribution      = returns
            };
        }
    }
}
