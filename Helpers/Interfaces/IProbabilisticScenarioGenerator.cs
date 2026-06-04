using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers.Interfaces
{
    /// <summary>
    /// Generates and executes Monte-Carlo style probabilistic macro scenarios.
    /// </summary>
    public interface IProbabilisticScenarioGenerator
    {
        /// <summary>Generates <paramref name="simulations"/> independent scenario draws.</summary>
        List<ProbabilisticScenario> Generate(int simulations);

        /// <summary>Runs the supplied scenarios through the scenario engine and returns the per-simulation results.</summary>
        Task<List<SimulationResult>> RunAsync(List<ProbabilisticScenario> scenarios);
    }
}
