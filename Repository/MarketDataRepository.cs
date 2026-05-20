using System.Text.Json;
using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Repository;

public sealed class MarketDataRepository(IDocumentStore store, ILogger<MarketDataRepository> log) : IMarketDataRepository
{
    public async Task UpsertCandlesAsync(string pair, string timeframe, List<Candle> candles, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeframe);
        ArgumentNullException.ThrowIfNull(candles);

        if (candles.Count == 0)
        {
            log.LogWarning("Skipping candle insert — empty list for {Pair}/{Tf}", pair, timeframe);
            return;
        }

        await using var session = store.LightweightSession();

        var id = CandleDocument.BuildId(pair, timeframe);

        var doc = await session.LoadAsync<CandleDocument>(id, ct);

        if (doc is null)
        {
            // Document doesn't exist, create new one with all candles
            doc = new CandleDocument
            {
                Id = id,
                Pair = pair,
                Timeframe = timeframe,
                Candles = candles.OrderBy(c => c.Time).ToList(),
                LastUpdated = DateTime.UtcNow
            };

            session.Insert(doc);
            await session.SaveChangesAsync(ct);

            log.LogInformation(
                "Inserted new document with {Count} candles for {Pair}/{Tf}",
                candles.Count,
                pair,
                timeframe);
            return;
        }

        // Document exists, only add new candles
        var existingTimes = doc.Candles
            .Select(c => c.Time)
            .ToHashSet();

        var newCandles = candles
            .Where(c => !existingTimes.Contains(c.Time))
            .ToList();

        if (newCandles.Count == 0)
        {
            log.LogDebug("No new candles to insert for {Pair}/{Tf}", pair, timeframe);
            return;
        }

        doc.Candles = doc.Candles
            .Concat(newCandles)
            .OrderBy(c => c.Time)
            .ToList();

        doc.LastUpdated = DateTime.UtcNow;

        // Note: Marten will track changes and update the existing document
        // This is an update operation, not an insert
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Added {New} new candles to existing document for {Pair}/{Tf} (total stored: {Total})",
            newCandles.Count,
            pair,
            timeframe,
            doc.Candles.Count);
    }

    public async Task<List<Candle>> GetCandlesAsync(string pair, string timeframe, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeframe);

        await using var session = store.QuerySession();

        var doc = await session.LoadAsync<CandleDocument>(
            CandleDocument.BuildId(pair, timeframe),
            ct);

        return doc?.Candles ?? [];
    }

    public async Task<(decimal Price, decimal ChangePercent)> GetLatestPriceAsync(string pair, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);

        var candles = await GetCandlesAsync(pair, "Daily", ct);

        if (candles.Count < 2)
            return (0, 0);

        var price = (decimal)candles[^1].Close;
        var previous = (decimal)candles[^2].Close;

        var change = previous > 0
            ? (price - previous) / previous * 100
            : 0;

        return (price, change);
    }

    public async Task InsertTradePlanAsync(TradePlan plan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        await using var session = store.LightweightSession();

        // Check if plan already exists to prevent duplicate inserts
        var existing = await session.LoadAsync<TradePlan>(plan.Id, ct);
        if (existing is not null)
        {
            log.LogWarning("TradePlan with Id={Id} already exists, skipping insert", plan.Id);
            return;
        }

        session.Insert(plan);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "TradePlan inserted: Id={Id} Pair={Pair} Direction={Direction} Entry={Entry} SL={SL} TP1={TP1} RR={RR:F2}",
            plan.Id,
            plan.Pair,
            plan.Direction,
            plan.EntryPrice,
            plan.StopLoss,
            plan.TakeProfit1,
            plan.RiskReward);
    }

    public async Task InsertTradePlansAsync(IEnumerable<TradePlan> plans, CancellationToken ct = default)
    {
        throw new NotImplementedException();
        // ArgumentNullException.ThrowIfNull(plans);
        //
        // var list = plans.ToList();
        //
        // if (list.Count == 0)
        //     return;
        //
        // await using var session = store.LightweightSession();
        //
        // // Filter out plans that already exist
        // var ids = list.Select(p => p.Id).ToHashSet();
        // var existingIds = await session.Query<TradePlan>()
        //     .Where(p => p.Id.IsOneOf(ids))
        //     .Select(p => p.Id)
        //     .ToListAsync(ct);
        //
        // var existingIdSet = existingIds.ToHashSet();
        // var newPlans = list.Where(p => !existingIdSet.Contains(p.Id)).ToList();
        //
        // if (newPlans.Count == 0)
        // {
        //     log.LogWarning("All {Count} trade plans already exist, skipping insert", list.Count);
        //     return;
        // }
        //
        // if (existingIdSet.Count > 0)
        // {
        //     log.LogWarning("Skipping {Count} duplicate trade plans", existingIdSet.Count);
        // }
        //
        // foreach (var plan in newPlans)
        // {
        //     session.Insert(plan);
        // }
        //
        // await session.SaveChangesAsync(ct);
        //
        // log.LogInformation("Inserted {Count} new trade plans (skipped {Skipped} duplicates)",
        //     newPlans.Count, existingIdSet.Count);
    }

    // Remove the duplicate method or delegate to the main one
    public Task InsertTradePlansAsync(List<TradePlan> plans, CancellationToken ct)
    {
        return InsertTradePlansAsync((IEnumerable<TradePlan>)plans, ct);
    }

    public async Task<IReadOnlyList<TradePlan>> GetTradePlansAsync(string pair, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);

        await using var session = store.QuerySession();

        return await session
            .Query<TradePlan>()
            .Where(p => p.Pair == pair)
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TradePlan>> GetPendingTradePlansAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        return await session
            .Query<TradePlan>()
            .Where(p => p.Status == TradePlanStatus.Pending.ToString())
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(ct);
    }

    public async Task UpsertMacroSnapshotsAsync(List<MacroSnapshot> snapshots, bool isLive, CancellationToken ct = default)
    {
        throw new NotImplementedException();
        // ArgumentNullException.ThrowIfNull(snapshots);
        //
        // if (snapshots.Count == 0)
        //     return;
        //
        // await using var session = store.LightweightSession();
        //
        // // Check for existing snapshots to avoid conflicts
        // var currencyIds = snapshots.Select(s => s.Currency).ToHashSet();
        // var existingDocs = await session.LoadManyAsync<MacroSnapshotDocument>(currencyIds, ct);
        // var existingCurrencies = existingDocs.Where(d => d is not null).Select(d => d.Currency).ToHashSet();
        //
        // var now = DateTime.UtcNow;
        // var newCount = 0;
        // var updateCount = 0;
        //
        // foreach (var snapshot in snapshots)
        // {
        //     if (existingCurrencies.Contains(snapshot.Currency))
        //     {
        //         // Document exists, update it
        //         var doc = existingDocs.First(d => d.Currency == snapshot.Currency);
        //         doc.Snapshot = snapshot;
        //         doc.IsLive = isLive;
        //         doc.LastUpdated = now;
        //         updateCount++;
        //     }
        //     else
        //     {
        //         // Insert new document
        //         session.Insert(new MacroSnapshotDocument
        //         {
        //             Id = snapshot.Currency,
        //             Snapshot = snapshot,
        //             IsLive = isLive,
        //             LastUpdated = now
        //         });
        //         newCount++;
        //     }
        // }
        //
        // await session.SaveChangesAsync(ct);
        //
        // log.LogInformation(
        //     "Processed {Total} macro snapshots: {New} inserted, {Updated} updated (live={IsLive})",
        //     snapshots.Count,
        //     newCount,
        //     updateCount,
        //     isLive);
    }

    public async Task<(List<MacroSnapshot> Data, bool IsLive)> GetMacroSnapshotsAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var docs = await session
            .Query<MacroSnapshotDocument>()
            .ToListAsync(ct);

        if (docs.Count == 0)
            return ([], false);

        return (
            docs.Select(d => d.Snapshot).ToList(),
            docs.Any(d => d.IsLive));
    }

    public async Task<MacroSnapshot?> GetMacroSnapshotAsync(string currency, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        await using var session = store.QuerySession();

        var doc = await session.LoadAsync<MacroSnapshotDocument>(currency, ct);

        return doc?.Snapshot;
    }

    public async Task UpsertSeriesAsync(string seriesId, List<EconomyDataPoint> points, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesId);
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0)
        {
            log.LogWarning("Skipping series upsert — empty data for {SeriesId}", seriesId);
            return;
        }

        await using var session = store.LightweightSession();

        var doc = await session.LoadAsync<EconomySeriesDocument>(seriesId, ct);

        if (doc is null)
        {
            // Insert new document with all points
            doc = new EconomySeriesDocument
            {
                Id = seriesId,
                DataPoints = points.OrderBy(p => p.Date).ToList(),
                LastUpdated = DateTime.UtcNow
            };

            session.Insert(doc);
            await session.SaveChangesAsync(ct);

            log.LogInformation(
                "Inserted new series {SeriesId} with {Count} points",
                seriesId,
                points.Count);
            return;
        }

        // Document exists, merge with existing data
        var incoming = points.ToDictionary(p => p.Date);
        var merged = doc.DataPoints
            .Where(p => !incoming.ContainsKey(p.Date))
            .Concat(incoming.Values)
            .OrderBy(p => p.Date)
            .ToList();

        doc.DataPoints = merged;
        doc.LastUpdated = DateTime.UtcNow;

        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Updated series {SeriesId}: added {New} new points (total: {Total})",
            seriesId,
            incoming.Count,
            merged.Count);
    }

    public async Task<List<EconomyDataPoint>> GetSeriesAsync(string seriesId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesId);

        await using var session = store.QuerySession();

        var doc = await session.LoadAsync<EconomySeriesDocument>(seriesId, ct);

        return doc?.DataPoints ?? [];
    }

    public Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var json = JsonSerializer.SerializeToDocument(payload);

        return SaveAsync(
            providerName,
            dataType,
            symbol,
            fromUtc,
            toUtc,
            json,
            cancellationToken);
    }

    public async Task<MarketDataSnapshot> SaveAsync(string providerName, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, JsonDocument payload, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(payload);

        if (toUtc < fromUtc)
            throw new ArgumentException("ToUtc cannot be earlier than FromUtc.", nameof(toUtc));

        var snapshot = new MarketDataSnapshot
        {
            Id = Guid.NewGuid(),
            Provider = providerName,
            DataType = dataType,
            Symbol = symbol,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Payload = payload,
            CreatedUtc = DateTime.UtcNow
        };

        await using var session = store.LightweightSession();

        // Check if snapshot with same parameters already exists
        var exists = await session.Query<MarketDataSnapshot>()
            .AnyAsync(x =>
                x.Symbol == symbol &&
                x.FromUtc == fromUtc &&
                x.ToUtc == toUtc &&
                x.Provider == providerName &&
                x.DataType == dataType,
                ct);

        if (exists)
        {
            log.LogWarning("MarketDataSnapshot already exists for {Symbol} {DataType} from {Provider}, skipping insert",
                symbol, dataType, providerName);
            return snapshot; // Return the snapshot even though it wasn't inserted
        }

        session.Insert(snapshot);
        await session.SaveChangesAsync(ct);

        log.LogInformation(
            "Saved {DataType} snapshot for {Symbol} from {Provider}",
            dataType,
            symbol,
            providerName);

        return snapshot;
    }

    public async Task<MarketDataSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        await using var session = store.QuerySession();

        return await session.LoadAsync<MarketDataSnapshot>(id, ct);
    }

    public async Task<IReadOnlyList<MarketDataSnapshot>> GetBySymbolAsync(
        string symbol,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (fromUtc.HasValue && toUtc.HasValue && toUtc.Value < fromUtc.Value)
            throw new ArgumentException("ToUtc cannot be earlier than FromUtc.", nameof(toUtc));

        await using var session = store.QuerySession();

        var query = session
            .Query<MarketDataSnapshot>()
            .Where(x => x.Symbol == symbol);

        if (fromUtc.HasValue)
            query = query.Where(x => x.FromUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.ToUtc <= toUtc.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MarketDataSnapshot>> GetByProviderAsync(
        string providerName,
        string? dataType = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        await using var session = store.QuerySession();

        var query = session
            .Query<MarketDataSnapshot>()
            .Where(x => x.Provider == providerName);

        if (!string.IsNullOrWhiteSpace(dataType))
            query = query.Where(x => x.DataType == dataType);

        return await query.ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (toUtc < fromUtc)
            throw new ArgumentException("ToUtc cannot be earlier than FromUtc.", nameof(toUtc));

        await using var session = store.QuerySession();

        return await session
            .Query<MarketDataSnapshot>()
            .AnyAsync(x =>
                    x.Symbol == symbol &&
                    x.FromUtc == fromUtc &&
                    x.ToUtc == toUtc,
                ct);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        await using var session = store.LightweightSession();

        var existing = await session.LoadAsync<MarketDataSnapshot>(id, ct);

        if (existing is null)
            return false;

        session.Delete(existing);
        await session.SaveChangesAsync(ct);

        return true;
    }
}