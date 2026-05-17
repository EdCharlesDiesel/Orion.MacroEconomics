using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
{
    public interface IProbabilisticScenarioGenerator
    {
        List<ProbabilisticScenario> Generate(int simulations);
        Task<List<SimulationResult>> RunAsync(List<ProbabilisticScenario> scenarios);
    }
}
