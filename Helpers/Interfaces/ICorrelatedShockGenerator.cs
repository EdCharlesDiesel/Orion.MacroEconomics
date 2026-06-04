using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers.Interfaces
{
    /// <summary>
    /// Generates jointly-distributed macroeconomic shocks (inflation, rate, growth, sentiment).
    /// </summary>
    public interface ICorrelatedShockGenerator
    {
        /// <summary>Generates a random correlated shock at baseline severity.</summary>
        ShockResult Generate();

        /// <summary>Generates a shock scaled by the supplied probabilistic context.</summary>
        ShockResult GenerateWithProbabilities(ProbabilisticScenarioResult probabilities);
    }
}
