using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IMarketDataEngine
{
    /// <summary>Persist a fetched market data payload and return a snapshot record.</summary>
    Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload, CancellationToken cancellationToken = default);

    /// <summary>Retrieve a snapshot by its unique id.</summary>
    Task<MarketDataSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieve all snapshots for a symbol, optionally filtered by date range.</summary>
    Task<IReadOnlyList<MarketDataSnapshot>> GetBySymbolAsync(string symbol, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieve all snapshots for a given provider and data type.</summary>
    Task<IReadOnlyList<MarketDataSnapshot>> GetByProviderAsync(string providerName, string? dataType = null, CancellationToken cancellationToken = default);

    /// <summary>Check whether a snapshot already exists for the given symbol and range.</summary>
    Task<bool> ExistsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Delete a snapshot by id. Returns true if it was found and deleted.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MarketTick?> GetLatestTickAsync(string pair, CancellationToken cancellationToken);
    Task<MacroData?> GetMacroDataAsync(CancellationToken cancellationToken);
    Task<MacroData?> RefreshMacroDataAsync(CancellationToken cancellationToken);
    Task<MarketDataHealth?> CheckHealthAsync(string pair, CancellationToken cancellationToken);
    Task<MarketDataSnapshot> FetchAndStoreAsync(string requestProvider, string requestSymbol, DateTime requestFromUtc, DateTime requestToUtc, CancellationToken cancellationToken);
}