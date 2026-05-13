using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Providers.Interfaces
{
    public interface IMarketDataFeedProvider
    {
        string Name { get; }

        Task<object> GetAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

        Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default);

        Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings();

        Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(MarketDataRequest request, CancellationToken cancellationToken = default);

        Task<MarketQuote?> GetLatestQuoteAsync(string pair, CancellationToken cancellationToken = default);

        Task<MarketTick?> GetLatestTickAsync(string pair, CancellationToken cancellationToken = default);

        Task<MarketDataHealth> CheckHealthAsync(string pair, CancellationToken cancellationToken = default);
    }
}

