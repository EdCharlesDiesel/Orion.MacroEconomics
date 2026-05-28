
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Repository;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;
using Orion.MacroEconomics.Strategies;
using Quartz;

namespace Orion.MacroEconomics.Jobs;

[DisallowConcurrentExecution]
public class DailyTradingStrategyJob : IJob
{
    private readonly ILogger<DailyTradingStrategyJob> _logger;
    private readonly MassiveClient _massiveClient;
    private readonly MarketDataRepository _marketRepo;
    private readonly DailySmaPivotStrategy _strategy;
    private readonly RiskManagementService _riskService;
    private readonly IRepository<StrategySignal> _signalRepo;
    private readonly IRepository<TradeExecution> _tradeRepo;
    private readonly IConfiguration _configuration;

    public DailyTradingStrategyJob(
        ILogger<DailyTradingStrategyJob> logger,
        MassiveClient massiveClient,
        MarketDataRepository marketRepo,
        DailySmaPivotStrategy strategy,
        RiskManagementService riskService,
        IRepository<StrategySignal> signalRepo,
        IRepository<TradeExecution> tradeRepo,
        IConfiguration configuration)
    {
        _logger = logger;
        _massiveClient = massiveClient;
        _marketRepo = marketRepo;
        _strategy = strategy;
        _riskService = riskService;
        _signalRepo = signalRepo;
        _tradeRepo = tradeRepo;
        _configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        try
        {
            _logger.LogInformation("Daily Trading Strategy Job started at {Time}", DateTime.UtcNow);

            // Get configured symbols
            var symbols = _configuration.GetSection("Strategy:Symbols").Get<List<string>>() ??
                         new List<string> { "EUR/USD", "GBP/USD", "USD/JPY", "AUD/USD" };

            var today = DateTime.UtcNow.Date;
            var state = await GetOrCreateDailyState(today);

            // Check if we can trade today
            if (state.MaxLossHit)
            {
                _logger.LogWarning("Max daily loss already hit for {Date}, skipping strategy", today);
                return;
            }

            var openTrades = await _tradeRepo.GetAsync(t => t.Status == "OPEN");
            var accountEquity = await GetAccountEquity();

            foreach (var symbol in symbols)
            {
                try
                {
                    // Get daily OHLC data (last 250 days for SMAs)
                    var dailyBars = await _massiveClient.GetDailyBarsAsync(symbol, 250, ct);
                    if (dailyBars == null || dailyBars.Count < 200)
                    {
                        _logger.LogWarning("Insufficient data for {Symbol}", symbol);
                        continue;
                    }

                    // Calculate pivot points from yesterday's data
                    var yesterday = dailyBars[dailyBars.Count - 2];
                    var pivots = CalculatePivots(yesterday.High, yesterday.Low, yesterday.Close);

                    // Calculate ATR
                    var atr = CalculateATR(dailyBars, 14);

                    // Generate signal
                    var signal = await _strategy.AnalyzeSignalAsync(symbol, dailyBars, pivots, atr, ct);

                    if (string.IsNullOrEmpty(signal.Direction))
                    {
                        _logger.LogDebug("No signal for {Symbol}", symbol);
                        continue;
                    }

                    // Validate trade
                    var validation = _riskService.ValidateTrade(
                        signal,
                        accountEquity,
                        state.TradesExecuted,
                        state.DailyPnL,
                        openTrades);

                    if (!validation.IsValid)
                    {
                        signal.Status = "REJECTED";
                        signal.RejectionReason = validation.Reason;
                        _logger.LogInformation("Signal for {Symbol} rejected: {Reason}", symbol, validation.Reason);
                    }
                    else
                    {
                        signal.Status = "EXECUTED";
                        await ExecuteTrade(signal, accountEquity);
                        state.TradesExecuted++;
                        _logger.LogInformation("Trade executed for {Symbol} at {Entry} with confidence {Confidence}%",
                            symbol, signal.Entry, signal.Confidence);
                    }

                    // Save signal
                    await _signalRepo.AddAsync(signal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing strategy for {Symbol}", symbol);
                }
            }

            // Update daily state
            await UpdateDailyState(state);

            _logger.LogInformation("Daily Trading Strategy Job completed. Executed {TradeCount} trades",
                state.TradesExecuted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Daily Trading Strategy Job");
        }
    }

    private PivotLevels CalculatePivots(decimal high, decimal low, decimal close)
    {
        var pivots = new PivotLevels();
        pivots.PP = (high + low + close) / 3;
        pivots.R1 = (2 * pivots.PP) - low;
        pivots.R2 = pivots.PP + (high - low);
        pivots.R3 = pivots.R2 + (high - low);
        pivots.S1 = (2 * pivots.PP) - high;
        pivots.S2 = pivots.PP - (high - low);
        pivots.S3 = pivots.S2 - (high - low);
        return pivots;
    }

    private decimal CalculateATR(List<OhlcvBar> bars, int period)
    {
        var trueRanges = new List<decimal>();

        for (int i = 1; i < bars.Count && i <= period; i++)
        {
            var tr = Math.Max(bars[i].High - bars[i].Low,
                Math.Max(Math.Abs(bars[i].High - bars[i-1].Close),
                         Math.Abs(bars[i].Low - bars[i-1].Close)));
            trueRanges.Add(tr);
        }

        return trueRanges.Average();
    }

    private async Task ExecuteTrade(StrategySignal signal, decimal accountEquity)
    {
        var riskPips = Math.Abs(signal.Entry - signal.StopLoss) *
                      (signal.Symbol.Contains("JPY") ? 100 : 10000);
        var positionSize = _riskService.CalculatePositionSize(accountEquity, riskPips);

        var trade = new TradeExecution
        {
            SignalId = signal.Id,
            Symbol = signal.Symbol,
            Direction = signal.Direction,
            EntryPrice = signal.Entry,
            PositionSize = positionSize,
            StopLoss = signal.StopLoss,
            TakeProfits = new Dictionary<string, decimal>
            {
                ["TP1"] = signal.TakeProfits[0],
                ["TP2"] = signal.TakeProfits[1],
                ["TP3"] = signal.TakeProfits[2]
            },
            EntryTime = DateTime.UtcNow
        };

        await _tradeRepo.AddAsync(trade);

        // Here you would integrate with your broker API to place actual trade
        // await _brokerService.PlaceOrderAsync(trade);
    }

    private async Task<DailyStrategyState> GetOrCreateDailyState(DateTime date)
    {
        // Implement storage/retrieval of daily state from database
        return new DailyStrategyState
        {
            Date = date,
            TradesExecuted = 0,
            DailyPnL = 0,
            MaxLossHit = false
        };
    }

    private async Task UpdateDailyState(DailyStrategyState state)
    {
        // Implement persistence of daily state
    }

    private async Task<decimal> GetAccountEquity()
    {
        // Get account balance from your system
        return 100000m; // Placeholder - $100,000 account
    }
}