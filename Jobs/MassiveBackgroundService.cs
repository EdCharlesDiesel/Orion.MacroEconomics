using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Extensions;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;

namespace Orion.MacroEconomics.Jobs;

public sealed class MassiveBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MassiveBackgroundService> log)
    : BackgroundService
{
    private static readonly string[] Pairs =
    [
        "C:EURUSD", "C:GBPUSD", "C:USDJPY", "C:USDCHF",
        "C:AUDUSD", "C:NZDUSD", "C:USDCAD", "C:USDZAR",
        "C:XAUUSD"
    ];

    private static readonly (string From, string To)[] QuotePairs =
    [
        ("EUR", "USD"), ("GBP", "USD"), ("USD", "JPY"), ("USD", "CHF"),
        ("AUD", "USD"), ("NZD", "USD"), ("USD", "CAD"), ("USD", "ZAR"),
        ("XAU", "USD")
    ];

    private DateTime _lastTickerSync     = DateTime.MinValue;
    private DateTime _lastMinuteSync     = DateTime.MinValue;
    private DateTime _lastFiveMinSync    = DateTime.MinValue;
    private DateTime _lastIndicatorSync  = DateTime.MinValue;
    private DateTime _lastH1Sync         = DateTime.MinValue;
    private DateTime _lastH4Sync         = DateTime.MinValue;
    private DateTime _lastDailySync      = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        log.LogInformation("MassiveForexBackgroundService starting");

        await RunStartupTasksAsync(ct);


        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            try
            {
                // ── 1 min: market status + last quotes + single snapshots ─────
                if ((now - _lastMinuteSync).TotalSeconds >= 60)
                {
                    await SyncMarketStatusAsync(ct);
                    await SyncLastQuotesAsync(ct);
                    await SyncSingleSnapshotsAsync(ct);
                    _lastMinuteSync = now;
                }

                // ── 5 min: full snapshot + top movers + conversions ───────────
                if ((now - _lastFiveMinSync).TotalMinutes >= 5)
                {
                    await SyncFullMarketSnapshotAsync(ct);
                    await SyncTopMoversAsync(ct);
                    await SyncCurrencyConversionsAsync(ct);
                    _lastFiveMinSync = now;
                }

                // ── 15 min: technical indicators ──────────────────────────────
                if ((now - _lastIndicatorSync).TotalMinutes >= 15)
                {
                    await SyncTechnicalIndicatorsAsync(ct);
                    _lastIndicatorSync = now;
                }

                // ── 1 hour: H1 candles + previous day bars ────────────────────
                if ((now - _lastH1Sync).TotalHours >= 1)
                {
                    await SyncCandlesAsync("hour", 1, 720, ct);
                    await SyncPreviousDayBarsAsync(ct);
                    _lastH1Sync = now;
                }

                // ── 4 hours: H4 candles ───────────────────────────────────────
                if ((now - _lastH4Sync).TotalHours >= 4)
                {
                    await SyncCandlesAsync("hour", 4, 365, ct);
                    _lastH4Sync = now;
                }

                // ── Daily: market summary + daily + weekly candles + trade plans
                if ((now - _lastDailySync).TotalHours >= 24 ||
                    (now.Hour == 0 && now.Minute <= 5 && _lastDailySync.Date < now.Date))
                {
                    await SyncDailyMarketSummaryAsync(ct);
                    await SyncCandlesAsync("day",  1, 365,  ct);
                    await SyncCandlesAsync("week", 1, 1095, ct);
                    await GenerateTradePlansAsync(ct);
                    _lastDailySync = now;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected error in MassiveForexBackgroundService loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }

        log.LogInformation("MassiveForexBackgroundService stopped");
    }

    // public async Task ExecuteDailyStrategyAsync()
    // {
    //
    //     var dailyData = await _massiveClient.GetDailyBarsAsync(symbol);
    //     var smaValues = new Dictionary<int, decimal>
    //     {
    //         [200] = CalculateSMA(dailyData, 200),
    //         [55] = CalculateSMA(dailyData, 55),
    //         [21] = CalculateSMA(dailyData, 21),
    //         [8] = CalculateSMA(dailyData, 8),
    //         [5] = CalculateSMA(dailyData, 5)
    //     };
    //
    //     // 3. Calculate pivot points
    //     var pivots = CalculateStandardPivots(dailyData.High, dailyData.Low, dailyData.Close);
    //
    //     // 4. Get ATR for volatility
    //     decimal atr = CalculateATR(dailyData, 14);
    //
    //     // 5. Generate signal
    //     var signal = await AnalyzeSignalAsync(symbol, smaValues, dailyData, pivots, atr);
    //
    //     // 6. Execute if confidence > 70%
    //     if (signal.Confidence >= 70 && _riskManager.ValidateTrade(signal, accountEquity, tradesToday, todayPnL))
    //     {
    //         await ExecuteTradeAsync(signal);
    //     }
    // }

    private async Task RunStartupTasksAsync(CancellationToken ct)
    {
        log.LogInformation("Running startup sync tasks");
        await SyncTickersAsync(ct);
        await SyncExchangesAsync(ct);
        _lastTickerSync = DateTime.UtcNow;
    }

    private async Task SyncTickersAsync(CancellationToken ct)
    {
        log.LogInformation("Syncing forex tickers");
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var tickers = await client.GetAllTickersAsync(ct);
            if (tickers.Count == 0) return;

            await using var session = store.LightweightSession();

            session.Store(new ForexReferenceDocument
            {
                Id = "forex-tickers",
                Tickers = tickers.Select(t => new ForexTickerRecord
                {
                    Ticker       = t.Ticker,
                    Name         = t.Name,
                    BaseCurrency = t.BaseCurrency,
                    Active       = t.Active
                }).ToList(),
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);
            log.LogInformation("Upserted {Count} forex tickers", tickers.Count);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to sync tickers");
        }
    }

    private async Task SyncExchangesAsync(CancellationToken ct)
    {
        log.LogInformation("Syncing forex exchanges");
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var exchanges = await client.GetExchangesAsync(ct: ct);
            if (exchanges.Count == 0) return;

            await using var session = store.LightweightSession();

            session.Store(new ForexExchangeDocument
            {
                Id = "forex-exchanges",
                Exchanges = exchanges.Select(e => new ExchangeRecord
                {
                    Id      = e.Id,
                    Name    = e.Name,
                    Acronym = e.Acronym ?? "",
                    Mic     = e.Mic     ?? "",
                    Type    = e.Type
                }).ToList(),
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);
            log.LogInformation("Upserted {Count} exchanges", exchanges.Count);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to sync exchanges");
        }
    }

    // ── 1-minute cadence ───────────────────────────────────────────────────

    private async Task SyncMarketStatusAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var status = await client.GetMarketStatusAsync(ct);
            if (status is null) return;

            var isFxOpen = status.Currencies?.Fx?.Equals("open",
                StringComparison.OrdinalIgnoreCase) ?? false;

            log.LogInformation("Market status: FX={FxStatus}", status.Currencies?.Fx ?? "unknown");

            await using var session = store.LightweightSession();

            session.Store(new MarketStatusDocument
            {
                Id         = "market-status",
                FxOpen     = isFxOpen,
                Market     = status.Market,
                ServerTime = status.ServerTime,
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);

            await SyncMarketHolidaysAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to sync market status");
        }
    }

    private async Task SyncMarketHolidaysAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var holidays = await client.GetMarketHolidaysAsync(ct);
            if (holidays.Count == 0) return;

            await using var session = store.LightweightSession();

            session.Store(new MarketHolidayDocument
            {
                Id = "market-holidays",
                Holidays = holidays.Select(h => new HolidayRecord
                {
                    Exchange = h.Exchange,
                    Name     = h.Name,
                    Date     = h.HolidayDate,
                    Status   = h.Status
                }).ToList(),
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to sync market holidays");
        }
    }

    private async Task SyncLastQuotesAsync(CancellationToken ct)
    {
        log.LogDebug("Syncing last quotes for {Count} pairs", QuotePairs.Length);

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.LightweightSession();

        foreach (var (from, to) in QuotePairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var quote = await client.GetLastQuoteAsync(from, to, ct);
                if (quote is null) continue;

                session.Store(new ForexQuoteDocument
                {
                    Id         = $"quote:{from}{to}",
                    Pair       = $"{from}/{to}",
                    Bid        = quote.Bid,
                    Ask        = quote.Ask,
                    Mid        = (quote.Bid + quote.Ask) / 2,
                    Exchange   = quote.Exchange,
                    QuoteTime  = DateTimeOffset.FromUnixTimeMilliseconds(quote.Timestamp).UtcDateTime,
                    UpdatedUtc = DateTime.UtcNow
                });

                log.LogDebug("{From}/{To} Bid={Bid} Ask={Ask}", from, to, quote.Bid, quote.Ask);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to get last quote for {From}/{To}", from, to);
            }
        }

        await session.SaveChangesAsync(ct);
    }

    private async Task SyncSingleSnapshotsAsync(CancellationToken ct)
    {
        log.LogDebug("Syncing single snapshots");

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.LightweightSession();

        foreach (var pair in Pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var snap = await client.GetSnapshotAsync(pair, ct);
                if (snap is null) continue;

                session.Store(MapSnapshot(snap));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to get snapshot for {Pair}", pair);
            }
        }

        await session.SaveChangesAsync(ct);
    }

    // ── 5-minute cadence ───────────────────────────────────────────────────

    private async Task SyncFullMarketSnapshotAsync(CancellationToken ct)
    {
        log.LogInformation("Syncing full market snapshot");
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var snapshots = await client.GetFullMarketSnapshotAsync(Pairs, ct);
            if (snapshots.Count == 0) return;

            await using var session = store.LightweightSession();

            foreach (var snap in snapshots)
                session.Store(MapSnapshot(snap));

            await session.SaveChangesAsync(ct);
            log.LogInformation("Stored {Count} market snapshots", snapshots.Count);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to sync full market snapshot");
        }
    }

    private async Task SyncTopMoversAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var gainers = await client.GetTopMoversAsync("gainers", ct);
            var losers  = await client.GetTopMoversAsync("losers",  ct);

            if (gainers.Count == 0 && losers.Count == 0) return;

            await using var session = store.LightweightSession();

            session.Store(new TopMoversDocument
            {
                Id         = "top-movers",
                Gainers    = gainers.Select(MapMover).ToList(),
                Losers     = losers.Select(MapMover).ToList(),
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);
            log.LogDebug("Top movers: {G} gainers, {L} losers", gainers.Count, losers.Count);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to sync top movers");
        }
    }

    private async Task SyncCurrencyConversionsAsync(CancellationToken ct)
    {
        var pairs = new[]
        {
            ("USD", "EUR"), ("USD", "GBP"), ("USD", "JPY"),
            ("USD", "ZAR"), ("EUR", "GBP"), ("GBP", "JPY")
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.LightweightSession();

        foreach (var (from, to) in pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var conv = await client.GetConversionAsync(from, to, 1, ct);
                if (conv is null) continue;

                session.Store(new CurrencyConversionDocument
                {
                    Id   = $"conversion:{from}{to}",
                    From = from,
                    To   = to,
                    Rate = conv.Last?.Bid > 0
                        ? (conv.Last.Ask + conv.Last.Bid) / 2
                        : conv.Converted,
                    Bid        = conv.Last?.Bid ?? 0,
                    Ask        = conv.Last?.Ask ?? 0,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed conversion {From}/{To}", from, to);
            }
        }

        await session.SaveChangesAsync(ct);
    }

    // ── 15-minute cadence ──────────────────────────────────────────────────

    private async Task SyncTechnicalIndicatorsAsync(CancellationToken ct)
    {
        log.LogInformation("Syncing technical indicators");

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.LightweightSession();

        foreach (var pair in Pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var sma20 = await client.GetSmaAsync(pair, "day", 20, limit: 5, ct: ct);
                var sma50 = await client.GetSmaAsync(pair, "day", 50, limit: 5, ct: ct);
                var ema20 = await client.GetEmaAsync(pair, "day", 20, limit: 5, ct: ct);
                var ema50 = await client.GetEmaAsync(pair, "day", 50, limit: 5, ct: ct);
                var rsi14 = await client.GetRsiAsync(pair, "day", 14, limit: 5, ct: ct);
                var macd  = await client.GetMacdAsync(pair, "day", limit: 5, ct: ct);

                var doc = new TechnicalIndicatorDocument();
                // {
                //     Id         = $"indicators:{pair}",
                //     Ticker     = pair,
                //     Sma20      = sma20?.Results.FirstOrDefault()?.Value ?? 0,
                //     Sma50      = sma50?.Results.FirstOrDefault()?.Value ?? 0,
                //     Ema20      = ema20?.Results.FirstOrDefault()?.Value ?? 0,
                //     Ema50      = ema50?.Results.FirstOrDefault()?.Value ?? 0,
                //     Rsi14      = rsi14?.Results.FirstOrDefault()?.Value ?? 0,
                //     MacdValue  = macd,
                //     MacdSignal = macd?.Results.FirstOrDefault()?.Signal ?? 0,
                //     MacdHist   = macd?.Results.FirstOrDefault()?.Histogram ?? 0,
                //     UpdatedUtc = DateTime.UtcNow
                // };

                session.Store(doc);

                log.LogDebug("{Pair} EMA20={E20:F5} RSI={RSI:F1} MACD={MACD:F5}",
                    pair, doc.Ema20, doc.Rsi14, doc.MacdValue);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed indicators for {Pair}", pair);
            }
        }

        await session.SaveChangesAsync(ct);
    }

    // ── Hourly / 4-hourly / daily cadence ──────────────────────────────────

    private async Task SyncCandlesAsync(string timespan, int multiplier, int lookbackDays, CancellationToken ct)
    {
        var label = $"{multiplier}{char.ToUpperInvariant(timespan[0])}";
        log.LogInformation("Syncing {Label} candles", label);

        var to   = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-lookbackDays);

        var timeframeLabel = (multiplier, timespan) switch
        {
            (1, "hour") => "Hourly",
            (4, "hour") => "4 Hour",
            (1, "day")  => "Daily",
            (1, "week") => "Weekly",
            _           => label
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

        foreach (var pair in Pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var agg = await client.GetCustomBarsAsync(
                    pair, multiplier, timespan, from, to, ct: ct);

                if (agg?.Results is null || agg.Results.Count == 0)
                {
                    log.LogDebug("No {Label} bars for {Pair}", label, pair);
                    continue;
                }

                var humanPair = pair.Replace("C:", "").Insert(3, "/");

                var candles = agg.Results
                    .Select(b => new Candle
                    {
                        Time   = b.TimestampUtc,
                        Open   = b.Open,
                        High   = b.High,
                        Low    = b.Low,
                        Close  = b.Close,
                        Volume = b.Volume
                    })
                    .ToList();

                await repo.UpsertCandlesAsync(humanPair, timeframeLabel, candles, ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed {Label} candles for {Pair}", label, pair);
            }
        }
    }

    private async Task SyncPreviousDayBarsAsync(CancellationToken ct)
    {
        log.LogInformation("Syncing previous day bars");

        await using var scope = scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        await using var session = store.LightweightSession();

        foreach (var pair in Pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var agg = await client.GetPreviousDayBarAsync(pair, ct: ct);
                var bar = agg?.Results?.FirstOrDefault();
                if (bar is null) continue;

                session.Store(new PreviousDayBarDocument
                {
                    Id         = $"prev-day:{pair}",
                    Ticker     = pair,
                    Open       = bar.Open,
                    High       = bar.High,
                    Low        = bar.Low,
                    Close      = bar.Close,
                    Volume     = bar.Volume,
                    Vwap       = bar.Vwap,
                    BarDate    = bar.TimestampUtc.Date,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed prev day bar for {Pair}", pair);
            }
        }

        await session.SaveChangesAsync(ct);
    }

    private async Task SyncDailyMarketSummaryAsync(CancellationToken ct)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        log.LogInformation("Syncing daily market summary for {Date}", yesterday);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<MassiveClient>();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            var summary = await client.GetDailyMarketSummaryAsync(yesterday, ct: ct);
            if (summary?.Results is null || summary.Results.Count == 0) return;

            await using var session = store.LightweightSession();

            session.Store(new DailyMarketSummaryDocument
            {
                Id   = $"daily-summary:{yesterday:yyyy-MM-dd}",
                Date = yesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Bars = summary.Results.Select(b => new DailySummaryBar
                {
                    Open      = b.Open,
                    High      = b.High,
                    Low       = b.Low,
                    Close     = b.Close,
                    Volume    = b.Volume,
                    Vwap      = b.Vwap,
                    Timestamp = b.TimestampUtc
                }).ToList(),
                UpdatedUtc = DateTime.UtcNow
            });

            await session.SaveChangesAsync(ct);
            log.LogInformation("Daily summary stored: {Count} tickers", summary.Results.Count);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to sync daily market summary");
        }
    }

    private async Task GenerateTradePlansAsync(CancellationToken ct)
    {
        log.LogInformation("Generating trade plans from Weekly candles");

        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
        var tradePlanFactory = scope.ServiceProvider.GetRequiredService<TradePlanFactory>();

        var plans   = new List<TradePlan>();
        var skipped = 0;

        foreach (var pair in Pairs)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var humanPair = pair.Replace("C:", "").Insert(3, "/");

                var candles = await repo.GetCandlesAsync(humanPair, "Weekly", ct);
                if (candles.Count == 0)
                {
                    log.LogDebug("{Pair}: no weekly candles found, skipping trade plan", humanPair);
                    skipped++;
                    continue;
                }

                var plan = tradePlanFactory.CreateFromCandles(humanPair, candles);
                if (plan is null)
                {
                    log.LogDebug("{Pair}: no trade plan signal", humanPair);
                    skipped++;
                    continue;
                }

                plans.Add(plan);

                log.LogInformation(
                    "Trade plan queued: {Pair} {Direction} Entry={Entry} SL={SL} TP1={TP1} RR={RR:F2}",
                    plan.Pair, plan.Direction, plan.EntryPrice, plan.StopLoss,
                    plan.TakeProfit1, plan.RiskReward);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to generate trade plan for {Pair}", pair);
            }
        }

        if (plans.Count > 0)
            await repo.InsertTradePlansAsync(plans, ct);

        log.LogInformation(
            "Trade plan generation complete: {Inserted} inserted, {Skipped} skipped",
            plans.Count, skipped);
    }

    // ── Mapping helpers ────────────────────────────────────────────────────

    private static ForexSnapshotDocument MapSnapshot(ForexSnapshot snap) => new()
    {
        Id              = $"snapshot:{snap.Ticker}",
        Ticker          = snap.Ticker,
        TodaysChange    = snap.TodaysChange,
        TodaysChangePct = snap.TodaysChangePct,
        DayOpen         = snap.Day?.Open      ?? 0,
        DayHigh         = snap.Day?.High      ?? 0,
        DayLow          = snap.Day?.Low       ?? 0,
        DayClose        = snap.Day?.Close     ?? 0,
        DayVolume       = snap.Day?.Volume    ?? 0,
        PrevClose       = snap.PrevDay?.Close ?? 0,
        Bid             = snap.LastQuote?.Bid ?? 0,
        Ask             = snap.LastQuote?.Ask ?? 0,
        UpdatedUtc      = DateTime.UtcNow
    };

    private static MoverRecord MapMover(ForexSnapshot snap) => new()
    {
        Ticker    = snap.Ticker,
        ChangePct = snap.TodaysChangePct,
        Change    = snap.TodaysChange,
        DayClose  = snap.Day?.Close ?? 0
    };
}