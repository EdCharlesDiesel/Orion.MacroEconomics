using System.Diagnostics;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;

namespace Orion.MacroEconomics.Jobs;

/// <summary>
/// Background service that periodically collects and stores forex data
/// using the Massive API client and repository pattern.
/// </summary>
public sealed class MassiveDataCollectionService : BackgroundService
{
    private readonly MassiveClient _client;
    private readonly IMassiveForexRepository _repository;
    private readonly ILogger<MassiveDataCollectionService> _log;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _snapshotInterval;
    private readonly TimeSpan _marketStatusInterval;
    private readonly TimeSpan _tickerRefreshInterval;
    private readonly TimeSpan _cleanupInterval;
    private readonly List<string> _monitoredTickers;

    public MassiveDataCollectionService(MassiveClient client, IMassiveForexRepository repository, ILogger<MassiveDataCollectionService> log, IConfiguration configuration)
    {
        _client = client;
        _repository = repository;
        _log = log;
        _configuration = configuration;

        // Read intervals from configuration with defaults
        _snapshotInterval = TimeSpan.FromSeconds(
            _configuration.GetValue("Massive:Collection:SnapshotIntervalSeconds", 60));
        _marketStatusInterval = TimeSpan.FromMinutes(
            _configuration.GetValue("Massive:Collection:MarketStatusIntervalMinutes", 5));
        _tickerRefreshInterval = TimeSpan.FromHours(
            _configuration.GetValue("Massive:Collection:TickerRefreshIntervalHours", 24));
        _cleanupInterval = TimeSpan.FromHours(
            _configuration.GetValue("Massive:Collection:CleanupIntervalHours", 1));

        // Default major forex pairs
        _monitoredTickers = _configuration
            .GetSection("Massive:MonitoredTickers")
            .Get<List<string>>() ?? new List<string>
            {
                "C:EURUSD", "C:GBPUSD", "C:USDJPY", "C:USDCHF",
                "C:AUDUSD", "C:USDCAD", "C:NZDUSD", "C:EURGBP",
                "C:EURJPY", "C:GBPJPY"
            };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Massive Data Collection Service started");
        _log.LogInformation("Monitoring {Count} tickers: {Tickers}",
            _monitoredTickers.Count, string.Join(", ", _monitoredTickers));

        try
        {
            // Initial data load
            await InitializeDataAsync(stoppingToken);

            // Create periodic tasks
            var tasks = new[]
            {
                CollectSnapshotsAsync(stoppingToken),
                CollectMarketStatusAsync(stoppingToken),
                RefreshTickersAsync(stoppingToken),
                CleanupOldDataAsync(stoppingToken)
            };

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _log.LogInformation("Massive Data Collection Service stopped");
        }
        catch (Exception ex)
        {
            _log.LogCritical(ex, "Fatal error in Massive Data Collection Service");
            throw;
        }
    }

    private async Task InitializeDataAsync(CancellationToken ct)
    {
        _log.LogInformation("Performing initial data load...");

        try
        {
            // Load tickers
            var tickers = await _client.GetAllTickersAsync(ct);
            if (tickers.Any())
            {
                await _repository.SaveTickersAsync(tickers, ct);
                _log.LogInformation("Loaded {Count} tickers", tickers.Count);
            }

            // Get initial market status
            var marketStatus = await _client.GetMarketStatusAsync(ct);
            if (marketStatus is not null)
            {
                await _repository.SaveMarketStatusAsync(marketStatus, ct);
                _log.LogInformation("Loaded initial market status");
            }

            // Get upcoming holidays
            var holidays = await _client.GetMarketHolidaysAsync(ct);
            if (holidays.Any())
            {
                await _repository.SaveMarketHolidaysAsync(holidays, ct);
                _log.LogInformation("Loaded {Count} market holidays", holidays.Count);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error during initial data load");
        }
    }

    private async Task CollectSnapshotsAsync(CancellationToken ct)
    {
        _log.LogInformation("Snapshot collection started (interval: {Interval}s)",
            _snapshotInterval.TotalSeconds);

        while (!ct.IsCancellationRequested)
        {
            var timer = Stopwatch.StartNew();
            var successCount = 0;
            var errorCount = 0;

            try
            {
                _log.LogDebug("Fetching snapshots for {Count} tickers", _monitoredTickers.Count);

                // Fetch snapshots in batches to avoid rate limiting
                var batchSize = 5;
                for (var i = 0; i < _monitoredTickers.Count; i += batchSize)
                {
                    if (ct.IsCancellationRequested) break;

                    var batch = _monitoredTickers.Skip(i).Take(batchSize);

                    try
                    {
                        var snapshots = await _client.GetFullMarketSnapshotAsync(batch, ct);

                        if (snapshots.Any())
                        {
                            await _repository.SaveSnapshotsAsync(snapshots, ct);
                            successCount += snapshots.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Error fetching snapshot batch starting at index {Index}", i);
                        errorCount++;
                    }

                    // Small delay between batches
                    if (i + batchSize < _monitoredTickers.Count)
                    {
                        await Task.Delay(2000, ct);
                    }
                }

                // Fetch technical indicators for key pairs
                await CollectTechnicalIndicatorsAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error in snapshot collection cycle");
                errorCount++;
            }

            timer.Stop();
            _log.LogInformation(
                "Snapshot collection cycle completed: {Success} successful, {Errors} errors in {Elapsed}ms",
                successCount, errorCount, timer.ElapsedMilliseconds);

            // Wait for next cycle
            var delay = _snapshotInterval - timer.Elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task CollectTechnicalIndicatorsAsync(CancellationToken ct)
    {
        // Collect indicators for a subset of major pairs
        var indicatorPairs = _monitoredTickers.Take(3);

        foreach (var ticker in indicatorPairs)
        {
            try
            {
                // SMA
                var sma = await _client.GetSmaAsync(ticker, "day", 20, "close", 5, ct);
                if (sma?.Results?.Any() == true)
                {
                    await _repository.SaveIndicatorAsync(ticker, "SMA", sma.Results, ct);
                    _log.LogDebug("Saved {Count} SMA values for {Ticker}", sma.Results.Count, ticker);
                }

                // RSI
                var rsi = await _client.GetRsiAsync(ticker, "day", 14, "close", 5, ct);
                if (rsi?.Results?.Any() == true)
                {
                    await _repository.SaveIndicatorAsync(ticker, "RSI", rsi.Results, ct);
                    _log.LogDebug("Saved {Count} RSI values for {Ticker}", rsi.Results.Count, ticker);
                }

                // Small delay between indicator requests
                await Task.Delay(1000, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error collecting indicators for {Ticker}", ticker);
            }
        }
    }

    private async Task CollectMarketStatusAsync(CancellationToken ct)
    {
        _log.LogInformation("Market status collection started (interval: {Interval}min)",
            _marketStatusInterval.TotalMinutes);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var status = await _client.GetMarketStatusAsync(ct);
                if (status is not null)
                {
                    await _repository.SaveMarketStatusAsync(status, ct);
                    _log.LogInformation("Market status updated: {Market}", status.Market);
                }

                var holidays = await _client.GetMarketHolidaysAsync(ct);
                if (holidays.Any())
                {
                    await _repository.SaveMarketHolidaysAsync(holidays, ct);
                    _log.LogInformation("Upcoming holidays updated: {Count}", holidays.Count);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error collecting market status");
            }

            await Task.Delay(_marketStatusInterval, ct);
        }
    }

    private async Task RefreshTickersAsync(CancellationToken ct)
    {
        _log.LogInformation("Ticker refresh started (interval: {Interval}h)",
            _tickerRefreshInterval.TotalHours);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tickers = await _client.GetAllTickersAsync(ct);
                if (tickers.Any())
                {
                    await _repository.SaveTickersAsync(tickers, ct);
                    _log.LogInformation("Tickers refreshed: {Count} total", tickers.Count);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error refreshing tickers");
            }

            await Task.Delay(_tickerRefreshInterval, ct);
        }
    }

    private async Task CleanupOldDataAsync(CancellationToken ct)
    {
        _log.LogInformation("Data cleanup started (interval: {Interval}h, retention: 30 days)",
            _cleanupInterval.TotalHours);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var deleted = await _repository.CleanupOldDataAsync(30, ct);
                if (deleted > 0)
                {
                    _log.LogInformation("Data cleanup completed: {Count} records deleted", deleted);
                }
                else
                {
                    _log.LogDebug("Data cleanup: no records to delete");
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error during data cleanup");
            }

            await Task.Delay(_cleanupInterval, ct);
        }
    }
}