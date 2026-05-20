
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Repository.Interfaces;

public interface IMassiveForexRepository
{

    Task SaveTickersAsync(List<ForexTicker> tickers, CancellationToken ct = default);
    Task<List<ForexTicker>> GetActiveTickersAsync(CancellationToken ct = default);
    Task<ForexTicker?> GetTickerAsync(string ticker, CancellationToken ct = default);
    Task SaveSnapshotAsync(ForexSnapshot snapshot, CancellationToken ct = default);
    Task SaveSnapshotsAsync(List<ForexSnapshot> snapshots, CancellationToken ct = default);
    Task<List<ForexSnapshot>> GetLatestSnapshotsAsync(IEnumerable<string>? tickers = null, CancellationToken ct = default);
    Task<ForexSnapshot?> GetLatestSnapshotAsync(string ticker, CancellationToken ct = default);
    Task<List<ForexSnapshot>> GetSnapshotHistoryAsync(string ticker, DateTime from, DateTime to, CancellationToken ct = default);
    Task SaveQuotesAsync(string ticker, List<ForexQuote> quotes, CancellationToken ct = default);
    Task<List<ForexQuote>> GetQuotesAsync(string ticker, DateTime? from = null, DateTime? to = null, int limit = 1000, CancellationToken ct = default);
    Task SaveConversionAsync(ConversionResult conversion, CancellationToken ct = default);
    Task<ConversionResult?> GetLatestConversionAsync(string from, string to, CancellationToken ct = default);
    Task SaveIndicatorAsync(string ticker, string indicatorType, List<IndicatorValue> values, CancellationToken ct = default);
    Task<List<IndicatorValue>> GetIndicatorAsync(string ticker, string indicatorType, DateTime? from = null, CancellationToken ct = default);
    Task SaveMarketStatusAsync(MarketStatus status, CancellationToken ct = default);
    Task<MarketStatus?> GetLatestMarketStatusAsync(CancellationToken ct = default);
    Task SaveMarketHolidaysAsync(List<MarketHoliday> holidays, CancellationToken ct = default);
    Task<List<MarketHoliday>> GetUpcomingHolidaysAsync(CancellationToken ct = default);
    Task<int> CleanupOldDataAsync(int retentionDays = 30, CancellationToken ct = default);
}