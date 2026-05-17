using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
{
    public interface IFredService
    {
        Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default);        
        Task<MacroData> RefreshMacroDataAsync( CancellationToken cancellationToken = default);
        Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings();
        Task<FredStatusResponse> CheckStatusAsync( CancellationToken cancellationToken = default);
        Task<object> GetCurrencyMacroDataAsync(string clean, CancellationToken cancellationToken);
    }
}
