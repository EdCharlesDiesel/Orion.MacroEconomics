using Marten;
using Marten.Linq;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Repository;

public sealed class MassiveForexRepository(IDocumentStore store, ILogger<MassiveForexRepository> log) : IMassiveForexRepository
{
    public async Task SaveTickersAsync(List<ForexTicker> tickers, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;
        var inserted = 0;
        var updated = 0;

        foreach (var ticker in tickers)
        {
            var existing = await session.LoadAsync<ForexTickerDocument>(ticker.Ticker, ct);

            if (existing is null)
            {
                session.Insert(new ForexTickerDocument
                {
                    Ticker = ticker.Ticker,
                    Name = ticker.Name,
                    BaseCurrency = ticker.BaseCurrency,
                    QuoteCurrency = ticker.QuoteCurrency,
                    Active = ticker.Active,
                    LastUpdated = now
                });
                inserted++;
            }
            else
            {
                existing.Name = ticker.Name;
                existing.BaseCurrency = ticker.BaseCurrency;
                existing.QuoteCurrency = ticker.QuoteCurrency;
                existing.Active = ticker.Active;
                existing.LastUpdated = now;
                updated++;
            }
        }

        await session.SaveChangesAsync(ct);
        log.LogInformation("Tickers saved: {Inserted} inserted, {Updated} updated", inserted, updated);
    }

    public async Task<List<ForexTicker>> GetActiveTickersAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var docs = await session
            .Query<ForexTickerDocument>()
            .Where(t => t.Active)
            .ToListAsync(ct);

        return docs.Select(d => new ForexTicker
        {
            Ticker = d.Ticker,
            Name = d.Name,
            BaseCurrency = d.BaseCurrency,
            QuoteCurrency = d.QuoteCurrency,
            Active = d.Active
        }).ToList();
    }

    public async Task<ForexTicker?> GetTickerAsync(string ticker, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();
        var doc = await session.LoadAsync<ForexTickerDocument>(ticker, ct);

        if (doc is null) return null;

        return new ForexTicker
        {
            Ticker = doc.Ticker,
            Name = doc.Name,
            BaseCurrency = doc.BaseCurrency,
            QuoteCurrency = doc.QuoteCurrency,
            Active = doc.Active
        };
    }

    public async Task SaveSnapshotAsync(ForexSnapshot snapshot, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();

        var doc = new ForexSnapshotDocument
        {
            Id = $"{snapshot.Ticker}_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Ticker = snapshot.Ticker,
            Snapshot = snapshot,
            CapturedAt = DateTime.UtcNow
        };

        session.Insert(doc);
        await session.SaveChangesAsync(ct);
    }

    public async Task SaveSnapshotsAsync(List<ForexSnapshot> snapshots, CancellationToken ct = default)
    {
        if (snapshots.Count == 0) return;

        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;

        foreach (var snapshot in snapshots)
        {
            var doc = new ForexSnapshotDocument
            {
                Id = $"{snapshot.Ticker}_{now:yyyyMMddHHmmss}",
                Ticker = snapshot.Ticker,
                Snapshot = snapshot,
                CapturedAt = now
            };
            session.Insert(doc);
        }

        await session.SaveChangesAsync(ct);
        log.LogInformation("Saved {Count} forex snapshots", snapshots.Count);
    }

    public async Task<List<ForexSnapshot>> GetLatestSnapshotsAsync(IEnumerable<string>? tickers = null, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var query = session.Query<ForexSnapshotDocument>();

        if (tickers?.Any() == true)
        {
            query = (query.Where(d => d.Ticker.IsOneOf(tickers.ToArray())) as IMartenQueryable<ForexSnapshotDocument>)!;
        }

        var docs = await query
            .OrderByDescending(d => d.CapturedAt)
            .ToListAsync(ct);

        // Get latest snapshot per ticker
        return docs
            .GroupBy(d => d.Ticker)
            .Select(g => g.First().Snapshot)
            .ToList();
    }

    public async Task<ForexSnapshot?> GetLatestSnapshotAsync(string ticker, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var doc = await session
            .Query<ForexSnapshotDocument>()
            .Where(d => d.Ticker == ticker)
            .OrderByDescending(d => d.CapturedAt)
            .FirstOrDefaultAsync(ct);

        return doc?.Snapshot;
    }

    public async Task<List<ForexSnapshot>> GetSnapshotHistoryAsync(string ticker, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var docs = await session
            .Query<ForexSnapshotDocument>()
            .Where(d => d.Ticker == ticker && d.CapturedAt >= from && d.CapturedAt <= to)
            .OrderBy(d => d.CapturedAt)
            .ToListAsync(ct);

        return docs.Select(d => d.Snapshot).ToList();
    }

    public async Task SaveQuotesAsync(string ticker, List<ForexQuote> quotes, CancellationToken ct = default)
    {
        if (quotes.Count == 0) return;

        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;

        foreach (var quote in quotes)
        {
            var doc = new ForexQuoteDocument
            {
                Id = $"{ticker}_{quote.SequenceNumber}",
                Ticker = ticker,
                Quote = quote,
                CapturedAt = now
            };
            session.Insert(doc);
        }

        await session.SaveChangesAsync(ct);
        log.LogInformation("Saved {Count} quotes for {Ticker}", quotes.Count, ticker);
    }

    public async Task<List<ForexQuote>> GetQuotesAsync(string ticker, DateTime? from = null, DateTime? to = null, int limit = 1000, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var query = session
            .Query<ForexQuoteDocument>()
            .Where(d => d.Ticker == ticker);

        if (from.HasValue)
            query = query.Where(d => d.CapturedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(d => d.CapturedAt <= to.Value);

        var docs = await query
            .OrderByDescending(d => d.CapturedAt)
            .Take(limit)
            .ToListAsync(ct);

        return docs.Select(d => d.Quote).ToList();
    }


    public async Task SaveConversionAsync(ConversionResult conversion, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();

        var doc = new ConversionDocument
        {
            Id = $"{conversion.From}_{conversion.To}_{conversion.Timestamp:yyyyMMddHHmmss}",
            From = conversion.From,
            To = conversion.To,
            Conversion = conversion,
            CapturedAt = DateTime.UtcNow
        };

        session.Insert(doc);
        await session.SaveChangesAsync(ct);
    }

    public async Task<ConversionResult?> GetLatestConversionAsync(string from, string to, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var doc = await session
            .Query<ConversionDocument>()
            .Where(d => d.From == from && d.To == to)
            .OrderByDescending(d => d.CapturedAt)
            .FirstOrDefaultAsync(ct);

        return doc?.Conversion;
    }

    public async Task SaveIndicatorAsync(string ticker, string indicatorType, List<IndicatorValue> values, CancellationToken ct = default)
    {
        if (values.Count == 0) return;

        await using var session = store.LightweightSession();
        var now = DateTime.UtcNow;

        foreach (var value in values)
        {
            var doc = new IndicatorDocument
            {
                Id = $"{ticker}_{indicatorType}_{value.Timestamp}",
                Ticker = ticker,
                IndicatorType = indicatorType,
                Value = value,
                CapturedAt = now
            };
            session.Insert(doc);
        }

        await session.SaveChangesAsync(ct);
        log.LogInformation("Saved {Count} {IndicatorType} values for {Ticker}", values.Count, indicatorType, ticker);
    }

    public async Task<List<IndicatorValue>> GetIndicatorAsync(string ticker, string indicatorType, DateTime? from = null, CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var query = session
            .Query<IndicatorDocument>()
            .Where(d => d.Ticker == ticker && d.IndicatorType == indicatorType);

        if (from.HasValue)
            query = query.Where(d => d.CapturedAt >= from.Value);

        var docs = await query
            .OrderByDescending(d => d.CapturedAt)
            .ToListAsync(ct);

        return docs.Select(d => d.Value).ToList();
    }


    public async Task SaveMarketStatusAsync(MarketStatus status, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();

        var doc = new MarketStatusDocument
        {
            Id = DateTime.UtcNow.ToString("yyyyMMdd"),
            Status = status,
            CapturedAt = DateTime.UtcNow
        };

        var existing = await session.LoadAsync<MarketStatusDocument>(doc.Id, ct);
        if (existing is not null)
        {
            existing.Status = status;
            existing.CapturedAt = DateTime.UtcNow;
        }
        else
        {
            session.Insert(doc);
        }

        await session.SaveChangesAsync(ct);
    }

    public async Task<MarketStatus?> GetLatestMarketStatusAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var doc = await session
            .Query<MarketStatusDocument>()
            .OrderByDescending(d => d.CapturedAt)
            .FirstOrDefaultAsync(ct);

        return doc?.Status;
    }

    public async Task SaveMarketHolidaysAsync(List<MarketHoliday> holidays, CancellationToken ct = default)
    {
        if (holidays.Count == 0) return;

        await using var session = store.LightweightSession();

        foreach (var holiday in holidays)
        {
            var doc = new MarketHolidayDocument
            {
                Id = $"{holiday.Exchange}_{holiday.HolidayDate:yyyyMMdd}",
                Exchange = holiday.Exchange,
                Holiday = holiday,
                HolidayDate = holiday.HolidayDate,  // Set the denormalized date
                CapturedAt = DateTime.UtcNow
            };

            var existing = await session.LoadAsync<MarketHolidayDocument>(doc.Id, ct);
            if (existing is null)
            {
                session.Insert(doc);
            }
            else
            {
                existing.Holiday = holiday;
                existing.HolidayDate = holiday.HolidayDate;  // Update denormalized date
                existing.CapturedAt = DateTime.UtcNow;
            }
        }

        await session.SaveChangesAsync(ct);
        // _log.LogInformation("Saved {Count} market holidays", holidays.Count);
    }

    public async Task<List<MarketHoliday>> GetUpcomingHolidaysAsync(CancellationToken ct = default)
    {
        await using var session = store.QuerySession();

        var docs = await session
            .Query<MarketHolidayDocument>()
            .Where(d => d.HolidayDate >= DateTime.UtcNow.Date)
            .OrderBy(d => d.HolidayDate)
            .ToListAsync(ct);

        return docs.Select(d => d.Holiday).ToList();
    }

    public async Task<int> CleanupOldDataAsync(int retentionDays = 30, CancellationToken ct = default)
    {
        await using var session = store.LightweightSession();
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = 0;

        // Clean up old snapshots
        var oldSnapshots = await session
            .Query<ForexSnapshotDocument>()
            .Where(d => d.CapturedAt < cutoff)
            .ToListAsync(ct);

        foreach (var doc in oldSnapshots)
        {
            session.Delete(doc);
            deleted++;
        }

        // Clean up old quotes
        var oldQuotes = await session
            .Query<ForexQuoteDocument>()
            .Where(d => d.CapturedAt < cutoff)
            .ToListAsync(ct);

        foreach (var doc in oldQuotes)
        {
            session.Delete(doc);
            deleted++;
        }

        if (deleted > 0)
        {
            await session.SaveChangesAsync(ct);
            log.LogInformation("Cleaned up {Count} old records (cutoff: {Cutoff:yyyy-MM-dd})", deleted, cutoff);
        }

        return deleted;
    }
}