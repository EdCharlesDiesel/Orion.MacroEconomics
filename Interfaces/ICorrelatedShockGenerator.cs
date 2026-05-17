using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
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