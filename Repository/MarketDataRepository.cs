using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Repository;

public sealed class MarketDataRepository(IDocumentStore store, ILogger<MarketDataRepository> log) : IMarketDataRepository
{
    public async Task UpsertCandlesAsync(string pair, string timeframe, List<Candle> candles, CancellationToken ct = default)
    {
        if (candles.Count == 0)
        {
            log.LogWarning("Skipping candle upsert — empty list for {Pair}/{Tf}", pair, timeframe);
            return;
        }

        await using var session = store.LightweightSession();

        var doc = await session.LoadAsync<CandleDocument>(CandleDocument.BuildId(pair, timeframe), ct)
                  ?? new CandleDocument { Id = CandleDocument.BuildId(pair, timeframe), Pair = pair, Timeframe = timeframe };

        // Merge: keep existing candles not in the new batch, add/replace new ones.
        var incoming = candles.ToDictionary(c => c.Time);
        var merged   = doc.Candles
            .Where(c => !incoming.ContainsKey(c.Time))
            .Concat(incoming.Values)
            .OrderBy(c => c.Time)
            .ToList();

        doc.Candles     = merged;
        doc.LastUpdated = DateTime.UtcNow;

        session.Store(doc);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Upserted {Count} candles for {Pair}/{Tf} (total stored: {Total})",
            candles.Count, pair, timeframe, merged.Count);
    }
    public async Task<List<Candle>> GetCandlesAsync(string pair, string timeframe, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<CandleDocument>(CandleDocument.BuildId(pair, timeframe), ct);
        return doc?.Candles ?? [];
    }

    public async Task<(decimal Price, decimal ChangePercent)> GetLatestPriceAsync(string pair, CancellationToken ct = default)
    {
        var candles = await GetCandlesAsync(pair, "Daily", ct);
        if (candles.Count < 2) return (0, 0);
        var price  = (decimal)candles[^1].Close;
        var prev   = (decimal)candles[^2].Close;
        var change = prev > 0 ? (price - prev) / prev * 100 : 0;
        return (price, change);
    }
    public async Task UpsertMacroSnapshotsAsync(List<MacroSnapshot> snapshots, bool isLive, CancellationToken ct = default)
    {

        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;
        foreach (var snap in snapshots)
        {
            var doc = new MacroSnapshotDocument
            {
                Id          = snap.Currency,
                Snapshot    = snap,
                IsLive      = isLive,
                LastUpdated = now,
            };
            session.Store(doc);
        }
        await session.SaveChangesAsync(ct);
        log.LogInformation("Upserted {Count} macro snapshots (live={IsLive})", snapshots.Count, isLive);
    }

    public async Task<(List<MacroSnapshot> Data, bool IsLive)> GetMacroSnapshotsAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var docs = await session.Query<MacroSnapshotDocument>().ToListAsync(ct);
        if (docs.Count == 0)
            return ([], false);

        var snapshots = docs.Select(d => d.Snapshot).ToList();
        var isLive    = docs.Any(d => d.IsLive);
        return (snapshots, isLive);
    }

    public async Task<MacroSnapshot?> GetMacroSnapshotAsync(string currency, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<MacroSnapshotDocument>(currency, ct);
        return doc?.Snapshot;
    }
    public async Task UpsertSeriesAsync(string seriesId, List<EconomyDataPoint> points, CancellationToken ct = default)
    {
        if (points.Count == 0)
        {
            log.LogWarning("Skipping series upsert — empty data for {SeriesId}", seriesId);
            return;
        }

        await using var session = store.LightweightSession();

        var doc = await session.LoadAsync<EconomySeriesDocument>(seriesId, ct)
                  ?? new EconomySeriesDocument { Id = seriesId };

        // Deduplicate by date, favouring incoming values.
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

    public async Task<List<EconomyDataPoint>> GetSeriesAsync(string seriesId, CancellationToken ct = default)
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
}