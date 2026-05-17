using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Providers.Interfaces;

public interface IAlphaVantageMarketDataProvider
{
    Task<IReadOnlyList<OhlcvBar>> GetDailyFxCandlesAsync(string pair, CancellationToken cancellationToken = default);
}