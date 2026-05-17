using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Providers.Interfaces
{
    public interface IYahooMarketProvider
    {
        Task<List<MarketDataResponse>> FetchDataAsync(string pair,string interval,string period,CancellationToken cancellationToken = default);
        Task<Dictionary<string, MarketDataResponse>> FetchAllTimeframesAsync(string pair,CancellationToken cancellationToken = default);
        Task<List<KpiData>> GetKpisAsync(CancellationToken cancellationToken = default);
    }    
}
