using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers.Interfaces
{
    /// <summary>
    /// Interface for correlated shock generator.
    /// </summary>
    public interface ICorrelatedShockGenerator
    {
        ShockResult Generate();
        ShockResult GenerateWithProbabilities(ProbabilisticScenarioResult probabilities);
    }
}