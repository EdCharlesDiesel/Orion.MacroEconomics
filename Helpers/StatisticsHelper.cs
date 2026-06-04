namespace Orion.MacroEconomics.Helpers
{
    public static class StatisticsHelper
    {
        public static decimal Mean(IEnumerable<decimal> values)
        {
            var list = values?.ToList() ?? new List<decimal>();
            return list.Count == 0 ? 0 : list.Average();
        }

        public static decimal StandardDeviation(IEnumerable<decimal> values)
        {
            var list = values?.ToList() ?? new List<decimal>();
            if (list.Count == 0) return 0;
            var mean = list.Average();
            return DecimalMath.Sqrt(list.Sum(v => (v - mean) * (v - mean)) / list.Count);
        }

        public static decimal Variance(IEnumerable<decimal> values)
        {
            var sd = StandardDeviation(values);
            return sd * sd;
        }

        public static decimal ZScore(IEnumerable<decimal> values, decimal current)
        {
            var list = values?.ToList() ?? new List<decimal>();
            if (list.Count == 0) return 0;
            var mean = list.Average();
            var std  = DecimalMath.Sqrt(list.Sum(v => (v - mean) * (v - mean)) / list.Count);

            return std == 0 ? 0 : (current - mean) / std;
        }

        public static decimal Percentile(IEnumerable<decimal> values, decimal percentile)
        {
            if (percentile < 0 || percentile > 100)
                throw new ArgumentOutOfRangeException(nameof(percentile));

            var sorted = values?.OrderBy(v => v).ToList() ?? new List<decimal>();
            if (sorted.Count == 0) return 0;
            if (sorted.Count == 1) return sorted[0];

            var rank = percentile / 100m * (sorted.Count - 1);
            var lo   = (int)Math.Floor(rank);
            var hi   = (int)Math.Ceiling(rank);
            if (lo == hi) return sorted[lo];

            var frac = rank - lo;
            return sorted[lo] + (sorted[hi] - sorted[lo]) * frac;
        }

        public static decimal Correlation(IEnumerable<decimal> a, IEnumerable<decimal> b)
        {
            var aa = a?.ToList() ?? new List<decimal>();
            var bb = b?.ToList() ?? new List<decimal>();
            var n  = Math.Min(aa.Count, bb.Count);
            if (n < 2) return 0;

            var meanA = aa.Take(n).Average();
            var meanB = bb.Take(n).Average();

            decimal cov = 0, varA = 0, varB = 0;
            for (int i = 0; i < n; i++)
            {
                var da = aa[i] - meanA;
                var db = bb[i] - meanB;
                cov  += da * db;
                varA += da * da;
                varB += db * db;
            }

            var denom = DecimalMath.Sqrt(varA * varB);
            return denom == 0 ? 0 : cov / denom;
        }
    }
}
