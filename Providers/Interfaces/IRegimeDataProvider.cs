
using Orion.MacroEconomics.DTO;

namespace Orion.MacroEconomics.Providers.Interfaces
{
    public interface IRegimeDataProvider
    {
        Task<RegimeInput> GetAsync(CancellationToken cancellationToken = default);
    }
}