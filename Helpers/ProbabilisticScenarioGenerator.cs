using Orion.MacroEconomics.Engine;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers.Interfaces;

namespace Orion.MacroEconomics.Helpers
{
    namespace Orion.API.TradingEconomics.Engine
    {
        public class ProbabilisticScenarioGenerator(ScenarioEngine scenarioEngine) : IProbabilisticScenarioGenerator
        {
            private readonly Random _rand = new();

            private const decimal CpiVolatility       = 0.02m;
            private const decimal RateVolatility      = 0.5m;
            private const decimal EuroCpiVolatility   = 0.015m;

            public List<ProbabilisticScenario> Generate(int simulations)
            {
                if (simulations <= 0)
                    return new List<ProbabilisticScenario>(0);

                var scenarios = new List<ProbabilisticScenario>(simulations);

                for (int i = 0; i < simulations; i++)
                {
                    scenarios.Add(new ProbabilisticScenario
                    {
                        SimulationId = i,
                        Shocks = new List<ScenarioShock>
                        {
                            GenerateShock("United States", "CPI",           CpiVolatility),
                            GenerateShock("United States", "Interest Rate", RateVolatility),
                            GenerateShock("Euro Area",     "CPI",           EuroCpiVolatility)
                        }
                    });
                }

                return scenarios;
            }

            public async Task<List<SimulationResult>> RunAsync(List<ProbabilisticScenario> scenarios)
            {
                if (scenarios == null || scenarios.Count == 0)
                    return new List<SimulationResult>(0);

                var results = new List<SimulationResult>(scenarios.Count);

                foreach (var s in scenarios)
                {
                    var scenario = new Scenario
                    {
                        Name   = $"Simulation {s.SimulationId}",
                        Shocks = s.Shocks
                    };

                    var scenarioResult  = await scenarioEngine.RunAsync(scenario);
                    var portfolioReturn = EstimateReturn(scenarioResult.Portfolio);
                    var risk            = EstimateRisk(scenarioResult.Portfolio);

                    results.Add(new SimulationResult
                    {
                        SimulationId    = s.SimulationId,
                        PortfolioReturn = portfolioReturn,
                        Risk            = risk,
                        Portfolio       = scenarioResult.Portfolio
                    });
                }

                return results;
            }

            private ScenarioShock GenerateShock(string country, string indicator, decimal volatility)
            {
                var shock = NextGaussian(0m, volatility);

                return new ScenarioShock
                {
                    Country    = country,
                    Indicator  = indicator,
                    ShockValue = shock,
                    Type       = ShockType.Relative
                };
            }

            private decimal NextGaussian(decimal mean, decimal stdDev)
            {
                var u1 = 1m - (decimal)_rand.NextDouble();
                var u2 = 1m - (decimal)_rand.NextDouble();

                var randStdNormal =
                    DecimalMath.Sqrt(-2m * DecimalMath.Log(u1)) *
                    DecimalMath.Sin(2m * DecimalMath.Pi * u2);

                return mean + stdDev * randStdNormal;
            }

            private static decimal EstimateReturn(List<PortfolioPosition> portfolio)
            {
                if (portfolio == null || portfolio.Count == 0)
                    return 0m;

                decimal total = 0m;
                foreach (var p in portfolio)
                {
                    var directionSign = string.Equals(p.Direction, "SHORT", StringComparison.OrdinalIgnoreCase)
                        ? -1m
                        :  1m;
                    var confidence = p.Confidence == 0m ? 1m : p.Confidence;
                    total += p.Weight * p.SignalStrength * directionSign * confidence;
                }
                return total;
            }

            private static decimal EstimateRisk(List<PortfolioPosition> portfolio)
            {
                if (portfolio == null || portfolio.Count == 0)
                    return 0m;

                var totalSize = portfolio.Sum(p => Math.Abs(p.PositionSize));
                decimal variance;

                if (totalSize > 0m)
                {
                    variance = portfolio.Sum(p =>
                    {
                        var sizeWeight = Math.Abs(p.PositionSize) / totalSize;
                        return sizeWeight * sizeWeight * p.Volatility;
                    });
                }
                else
                {
                    variance = portfolio.Sum(p => p.Weight * p.Weight * p.Volatility);
                }

                return DecimalMath.Sqrt(variance);
            }
        }
    }
}
