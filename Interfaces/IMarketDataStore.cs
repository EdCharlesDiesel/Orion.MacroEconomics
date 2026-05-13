using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces;

public interface IMarketDataStore
{
    Task<MarketDataSnapshot> SaveAsync(string provider, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload, CancellationToken cancellationToken = default);
}