using System.Text.Json;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Repository.Interfaces;

public interface IMarketDataRepository
{
    Task UpsertCandlesAsync(string pair, string timeframe, List<Candle> candles, CancellationToken ct = default);
    Task<List<Candle>> GetCandlesAsync(string pair, string timeframe, CancellationToken ct = default);
    Task<(decimal Price, decimal ChangePercent)> GetLatestPriceAsync(string pair, CancellationToken ct = default);
    Task UpsertMacroSnapshotsAsync(List<MacroSnapshot> snapshots, bool isLive, CancellationToken ct = default);
    Task<(List<MacroSnapshot> Data, bool IsLive)> GetMacroSnapshotsAsync(CancellationToken ct = default);
    Task<MacroSnapshot?> GetMacroSnapshotAsync(string currency, CancellationToken ct = default);
    Task UpsertSeriesAsync(string seriesId, List<EconomyDataPoint> points, CancellationToken ct = default);
    Task<List<EconomyDataPoint>> GetSeriesAsync(string seriesId, CancellationToken ct = default);
    Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload, CancellationToken cancellationToken);
    Task<MarketDataSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketDataSnapshot>> GetBySymbolAsync(string symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<MarketDataSnapshot>> GetByProviderAsync(string providerName, string? dataType, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task InsertTradePlansAsync(List<TradePlan> plans, CancellationToken ct);
}