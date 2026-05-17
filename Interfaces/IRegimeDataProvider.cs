
using Orion.MacroEconomics.DTO;

namespace Orion.MacroEconomics.Interfaces
{
    public interface IRegimeDataProvider
    {
        Task<RegimeInput> GetAsync(CancellationToken cancellationToken = default);
    }
}