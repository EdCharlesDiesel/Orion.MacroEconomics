using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers.Interfaces;

namespace Orion.MacroEconomics.Helpers;

public class CorrelatedShockGenerator : ICorrelatedShockGenerator
{
    private readonly Random _rand = new();

    private readonly decimal[,] _cov =
    {
        {  0.02m,   0.01m,  -0.005m },
        {  0.01m,   0.03m,  -0.002m },
        { -0.005m, -0.002m,  0.025m }
    };

    private readonly decimal[,] _cholesky;

    public CorrelatedShockGenerator()
    {
        _cholesky = CholeskyDecompose(_cov);
    }

    public ShockResult Generate()
    {
        var (inf, rate, growth) = GenerateCorrelated(scale: 1m);

        return new ShockResult
        {
            InflationShock = inf,
            RateShock      = rate,
            SentimentShock = rate,
            GrowthShock    = growth
        };
    }

    public ShockResult GenerateWithProbabilities(ProbabilisticScenarioResult probabilities)
    {
        var scale = probabilities is null || probabilities.Probability <= 0
            ? 1m
            : Math.Clamp(probabilities.Probability, 0.1m, 3m);

        var (inf, rate, growth) = GenerateCorrelated(scale);

        return new ShockResult
        {
            InflationShock = inf,
            RateShock      = rate,
            SentimentShock = rate,
            GrowthShock    = growth
        };
    }

    private (decimal Inflation, decimal Rate, decimal Growth) GenerateCorrelated(decimal scale)
    {
        var z = new[] { NextGaussian(), NextGaussian(), NextGaussian() };

        var inflation = (_cholesky[0, 0] * z[0]) * scale;
        var rate      = (_cholesky[1, 0] * z[0] + _cholesky[1, 1] * z[1]) * scale;
        var growth    = (_cholesky[2, 0] * z[0] + _cholesky[2, 1] * z[1] + _cholesky[2, 2] * z[2]) * scale;

        return (inflation, rate, growth);
    }

    private decimal NextGaussian()
    {
        var u1 = 1m - (decimal)_rand.NextDouble();
        var u2 = 1m - (decimal)_rand.NextDouble();
        return DecimalMath.Sqrt(-2m * DecimalMath.Log(u1)) * DecimalMath.Sin(2m * DecimalMath.Pi * u2);
    }

    private static decimal[,] CholeskyDecompose(decimal[,] m)
    {
        var n = m.GetLength(0);
        var l = new decimal[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                decimal sum = 0m;
                for (int k = 0; k < j; k++)
                    sum += l[i, k] * l[j, k];

                if (i == j)
                {
                    var diag = m[i, i] - sum;
                    if (diag <= 0m) diag = 0.000001m;
                    l[i, j] = DecimalMath.Sqrt(diag);
                }
                else
                {
                    l[i, j] = l[j, j] == 0m ? 0m : (m[i, j] - sum) / l[j, j];
                }
            }
        }
        return l;
    }
}
