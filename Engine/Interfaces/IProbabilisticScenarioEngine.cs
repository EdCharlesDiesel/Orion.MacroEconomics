using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Runs and calculates probabilistic scenario outcomes.
    /// </summary>
    public interface IProbabilisticScenarioEngine
    {
        /// <summary>
        /// Runs multiple probabilistic scenarios.
        /// </summary>
        Task<List<SimulationResult>> RunAsync(List<ProbabilisticScenario> scenarios);

        /// <summary>
        /// Calculates probability for a normalized macro scenario.
        /// </summary>
        ProbabilisticScenarioResult Calculate(NormalizedIndicator normalized, RegimeResult regime, ScenarioResult scenario);
    }
}