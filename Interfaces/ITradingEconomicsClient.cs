using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
{
    public interface ITradingEconomicsClient
    {
        Task<IEnumerable<EconomicIndicator>> GetIndicatorsAsync(string country);
    }
}
