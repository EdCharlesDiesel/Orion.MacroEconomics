using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Provides access to macro and market data used by the trading system.
    /// </summary>
    public interface IMarketDataEngine
    {
        Task<MarketDataSnapshot> FetchAndStoreAsync(string provider, string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
        
        Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default);

        Task<MacroData> RefreshMacroDataAsync(CancellationToken cancellationToken = default);

        Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings();

        Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(MarketDataRequest request, CancellationToken cancellationToken = default);

        Task<MarketQuote?> GetLatestQuoteAsync(string pair, CancellationToken cancellationToken = default);
        
        Task<MarketDataHealth> CheckHealthAsync(string pair, CancellationToken cancellationToken = default);

        Task<MarketTick> GetLatestTickAsync(string pair, CancellationToken cancellationToken);
    }
}