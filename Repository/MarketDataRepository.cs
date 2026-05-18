using System.Text.Json;
using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Repository;

public sealed class MarketDataRepository(    IDocumentStore store, ILogger<MarketDataRepository> log) : IMarketDataRepository
{
    // ── Candles ────────────────────────────────────────────────────────────────
    public async Task UpsertCandlesAsync(string pair, string timeframe, List<Candle> candles, CancellationToken ct = default)
    {
        if (candles.Count == 0)
        {
            log.LogWarning("Skipping candle upsert — empty list for {Pair}/{Tf}", pair, timeframe);
            return;
        }

        await using var session = store.LightweightSession();

        var doc = await session.LoadAsync<CandleDocument>(
                      CandleDocument.BuildId(pair, timeframe), ct)
                  ?? new CandleDocument
                  {
                      Id        = CandleDocument.BuildId(pair, timeframe),
                      Pair      = pair,
                      Timeframe = timeframe
                  };

        var incoming = candles.ToDictionary(c => c.Time);
        var merged   = doc.Candles
            .Where(c => !incoming.ContainsKey(c.Time))
            // .Concat(incoming.Values)
            // .OrderBy(c => c.Time)
            .ToList();

        doc.Candles     = merged;
        doc.LastUpdated = DateTime.UtcNow;

        session.Store(doc);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Upserted {Count} candles for {Pair}/{Tf} (total: {Total})",
            candles.Count, pair, timeframe, merged.Count);
    }

    public async Task<List<Extensions.Candle>> GetCandlesAsync(string pair, string timeframe,
        CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<CandleDocument>(
            CandleDocument.BuildId(pair, timeframe), ct);
        return doc?.Candles ?? [];
    }

    public async Task<(decimal Price, decimal ChangePercent)> GetLatestPriceAsync(
        string pair, CancellationToken ct = default)
    {
        var candles = await GetCandlesAsync(pair, "Daily", ct);
        if (candles.Count < 2) return (0, 0);
        var price  = (decimal)candles[^1].Close;
        var prev   = (decimal)candles[^2].Close;
        var change = prev > 0 ? (price - prev) / prev * 100 : 0;
        return (price, change);
    }

    // ── Macro snapshots ────────────────────────────────────────────────────────
    public async Task UpsertMacroSnapshotsAsync(
        List<MacroSnapshot> snapshots, bool isLive,
        CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;

        foreach (var snap in snapshots)
        {
            session.Store(new MacroSnapshotDocument
            {
                Id          = snap.Currency,
                Snapshot    = snap,
                IsLive      = isLive,
                LastUpdated = now
            });
        }

        await session.SaveChangesAsync(ct);
        log.LogInformation(
            "Upserted {Count} macro snapshots (live={IsLive})",
            snapshots.Count, isLive);
    }

    public async Task<(List<MacroSnapshot> Data, bool IsLive)> GetMacroSnapshotsAsync(
        CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var docs = await session.Query<MacroSnapshotDocument>().ToListAsync(ct);
        if (docs.Count == 0) return ([], false);
        return (docs.Select(d => d.Snapshot).ToList(), docs.Any(d => d.IsLive));
    }

    public async Task<MacroSnapshot?> GetMacroSnapshotAsync(
        string currency, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<MacroSnapshotDocument>(currency, ct);
        return doc?.Snapshot;
    }

    // ── Economy series ─────────────────────────────────────────────────────────
    public async Task UpsertSeriesAsync(
        string seriesId, List<EconomyDataPoint> points,
        CancellationToken ct = default)
    {
        if (points.Count == 0)
        {
            log.LogWarning("Skipping series upsert — empty data for {SeriesId}", seriesId);
            return;
        }

        await using var session = store.LightweightSession();

        var doc = await session.LoadAsync<EconomySeriesDocument>(seriesId, ct)
                  ?? new EconomySeriesDocument { Id = seriesId };

        var incoming = points.ToDictionary(p => p.Date);
        var merged   = doc.DataPoints
            .Where(p => !incoming.ContainsKey(p.Date))
            .Concat(incoming.Values)
            .OrderBy(p => p.Date)
            .ToList();

        doc.DataPoints  = merged;
        doc.LastUpdated = DateTime.UtcNow;

        session.Store(doc);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Upserted {Count} points for series {SeriesId} (total: {Total})",
            points.Count, seriesId, merged.Count);
    }

    public async Task<List<EconomyDataPoint>> GetSeriesAsync(
        string seriesId, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<EconomySeriesDocument>(seriesId, ct);
        return doc?.DataPoints ?? [];
    }

    public Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    // ── Snapshots (market data engine storage) ─────────────────────────────────
    public async Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol,
        DateTime fromUtc, DateTime toUtc,
        JsonDocument payload, CancellationToken ct)
    {
        var snapshot = new MarketDataSnapshot
        {
            Id          = Guid.NewGuid(),
            Provider    = providerName,
            DataType    = dataType,
            Symbol      = symbol,
            FromUtc     = fromUtc,
            ToUtc       = toUtc,
            Payload     = payload,
            CreatedUtc  = DateTime.UtcNow
        };

        await using var session = store.LightweightSession();
        session.Store(snapshot);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Saved {DataType} snapshot for {Symbol} from {Provider}",
            dataType, symbol, providerName);

        return snapshot;
    }

    public async Task<MarketDataSnapshot?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        return await session.LoadAsync<MarketDataSnapshot>(id, ct);
    }

    public async Task<IReadOnlyList<MarketDataSnapshot>> GetBySymbolAsync(
        string symbol, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var query = session.Query<MarketDataSnapshot>().Where(x => x.Symbol == symbol);
        if (fromUtc.HasValue) query = query.Where(x => x.FromUtc >= fromUtc.Value);
        if (toUtc.HasValue)   query = query.Where(x => x.ToUtc   <= toUtc.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MarketDataSnapshot>> GetByProviderAsync(
        string providerName, string? dataType = null,
        CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var query = session.Query<MarketDataSnapshot>()
            .Where(x => x.Provider == providerName);
        if (!string.IsNullOrWhiteSpace(dataType))
            query = query.Where(x => x.DataType == dataType);
        return await query.ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(
        string symbol, DateTime fromUtc, DateTime toUtc,
        CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        return await session.Query<MarketDataSnapshot>()
            .AnyAsync(x => x.Symbol  == symbol
                        && x.FromUtc == fromUtc
                        && x.ToUtc   == toUtc, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();
        var existing = await session.LoadAsync<MarketDataSnapshot>(id, ct);
        if (existing is null) return false;
        session.Delete(existing);
        await session.SaveChangesAsync(ct);
        return true;
    }
}