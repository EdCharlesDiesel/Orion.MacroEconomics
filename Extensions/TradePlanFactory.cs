using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using LiveTradingConfig = Orion.MacroEconomics.Configuration.LiveTradingConfig;

namespace Orion.MacroEconomics.Extensions;

public sealed class TradePlanFactory(IOptions<AppConfiguration> config, ILogger<TradePlanFactory> logger)
{
    private const int AtrPeriod = 14;
    private const int RsiPeriod = 14;
    private const int Ema20Period = 20;
    private const int Ema50Period = 50;
    private const int KeyLevelLookback = 20;

    private readonly AppConfiguration _cfg = config?.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<TradePlanFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public TradePlan? CreateFromCandles(string pair, List<Candle> candles)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("Pair is required.", nameof(pair));

        if (candles is null)
            throw new ArgumentNullException(nameof(candles));

        var minimumCandlesRequired = Math.Max(Ema50Period, Math.Max(AtrPeriod + 1, RsiPeriod + 1));

        if (candles.Count < minimumCandlesRequired)
        {
            _logger.LogWarning(
                "Not enough candles for {Pair}. Need at least {Required}, have {Count}",
                pair,
                minimumCandlesRequired,
                candles.Count);

            return null;
        }

        var sorted = candles
            .Where(c => c.High > 0 && c.Low > 0 && c.Close > 0)
            .OrderBy(c => c.Time)
            .ToList();

        if (sorted.Count < minimumCandlesRequired)
        {
            _logger.LogWarning(
                "Not enough valid candles for {Pair}. Need at least {Required}, have {Count}",
                pair,
                minimumCandlesRequired,
                sorted.Count);

            return null;
        }

        var current = sorted[^1];

        var atr = CalculateAtr(sorted, AtrPeriod);
        var ema20 = CalculateEma(sorted, Ema20Period);
        var ema50 = CalculateEma(sorted, Ema50Period);
        var rsi = CalculateRsi(sorted, RsiPeriod);
        var (support, resistance) = FindKeyLevels(sorted, KeyLevelLookback);

        if (atr <= 0)
        {
            _logger.LogWarning("{Pair} ATR is zero or invalid. Skipping trade plan.", pair);
            return null;
        }

        if (support <= 0 || resistance <= 0 || resistance <= support)
        {
            _logger.LogWarning(
                "{Pair} invalid key levels. Support={Support}, Resistance={Resistance}",
                pair,
                support,
                resistance);

            return null;
        }

        var close = (decimal)current.Close;

        _logger.LogInformation(
            "{Pair} | Close={Close} EMA20={EMA20:F5} EMA50={EMA50:F5} ATR={ATR:F5} RSI={RSI:F2} Support={Support:F5} Resistance={Resistance:F5}",
            pair,
            close,
            ema20,
            ema50,
            atr,
            rsi,
            support,
            resistance);

        var direction = DetermineDirection(
            close,
            ema20,
            ema50,
            rsi,
            (decimal)support,
            (decimal)resistance);

        if (direction == nameof(TradeDirection.None))
        {
            _logger.LogInformation("{Pair} has no valid directional signal. Skipping.", pair);
            return null;
        }

        var pairCfg = GetPairConfig(pair);

        var atrStopMultiplier = pairCfg?.AtrStopMultiplier > 0
            ? pairCfg.AtrStopMultiplier
            : _cfg.ATRSLMult;

        var minStopDistance = pairCfg?.MinStopDistance > 0
            ? pairCfg.MinStopDistance
            : _cfg.DefaultMinStopDistance;

        if (atrStopMultiplier <= 0)
            atrStopMultiplier = 1.5m;

        if (minStopDistance <= 0)
            minStopDistance = 0.0010m;

        var stopDistance = Math.Max((decimal)atr * atrStopMultiplier, minStopDistance);

        var entry = close;

        var stopLoss = direction == nameof(TradeDirection.Long)
            ? entry - stopDistance
            : entry + stopDistance;

        var takeProfit1 = direction == nameof(TradeDirection.Long)
            ? entry + stopDistance * _cfg.TP1ATRMult
            : entry - stopDistance * _cfg.TP1ATRMult;

        var takeProfit2 = direction == nameof(TradeDirection.Long)
            ? entry + stopDistance * _cfg.TP2ATRMult
            : entry - stopDistance * _cfg.TP2ATRMult;

        var riskReward = Math.Abs(takeProfit1 - entry) / stopDistance;

        if (riskReward < _cfg.MinRR)
        {
            _logger.LogInformation(
                "{Pair} R:R {RiskReward:F2} below minimum {MinimumRiskReward:F2}. Skipping.",
                pair,
                riskReward,
                _cfg.MinRR);

            return null;
        }

        var plan = new TradePlan
        {
            Id = Guid.NewGuid(),
            Pair = pair,
            Direction = direction,
            Status = nameof(TradePlanStatus.Pending),
            OpenedAt = DateTime.UtcNow,

            EntryPrice = RoundPrice(entry),
            StopLoss = RoundPrice(stopLoss),
            TakeProfit1 = RoundPrice(takeProfit1),
            TakeProfit2 = RoundPrice(takeProfit2),

            RiskReward = Math.Round(riskReward, 2),
            ATR = Math.Round(atr, 5),

            EMA20 = Math.Round(ema20, 5),
            EMA50 = Math.Round(ema50, 5),
            RSI = Math.Round(rsi, 2),
            Support = Math.Round(support, 5),
            Resistance = Math.Round(resistance, 5),

            Timeframe = "Weekly",
            Reasoning = BuildReasoning(
                pair,
                direction,
                close,
                ema20,
                ema50,
                rsi,
                atr,
                riskReward)
        };

        _logger.LogInformation(
            "TradePlan created for {Pair} | Direction={Direction} Entry={Entry} SL={StopLoss} TP1={TakeProfit1} TP2={TakeProfit2} RR={RiskReward:F2}",
            pair,
            direction,
            plan.EntryPrice,
            plan.StopLoss,
            plan.TakeProfit1,
            plan.TakeProfit2,
            plan.RiskReward);

        return plan;
    }

    private static decimal CalculateAtr(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
            return 0;

        var trueRanges = new List<decimal>();

        for (var i = 1; i < candles.Count; i++)
        {
            var high = candles[i].High;
            var low = candles[i].Low;
            var previousClose = candles[i - 1].Close;

            var highLow = high - low;
            var highPreviousClose = Math.Abs(high - previousClose);
            var lowPreviousClose = Math.Abs(low - previousClose);

            trueRanges.Add(Math.Max(highLow, Math.Max(highPreviousClose, lowPreviousClose)));
        }

        return trueRanges
            .TakeLast(period)
            .Average();
    }

    private static decimal CalculateEma(List<Candle> candles, int period)
    {
        if (candles.Count < period)
            return 0;

        var multiplier = 2m / (period + 1);
        var ema = (decimal)candles.Take(period).Average(c => c.Close);

        foreach (var candle in candles.Skip(period))
        {
            var close = (decimal)candle.Close;
            ema = ((close - ema) * multiplier) + ema;
        }

        return ema;
    }

    private static decimal CalculateRsi(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
            return 50;

        var recent = candles
            .TakeLast(period + 1)
            .ToList();

        var gains = 0.0m;
        var losses = 0.0m;

        for (var i = 1; i < recent.Count; i++)
        {
            var change = recent[i].Close - recent[i - 1].Close;

            if (change > 0)
                gains += change;
            else
                losses += Math.Abs(change);
        }

        var averageGain = gains / period;
        var averageLoss = losses / period;

        if (averageLoss == 0)
            return 100;

        var relativeStrength = averageGain / averageLoss;

        return 100 - 100 / (1 + relativeStrength);
    }

    private static (decimal Support, decimal Resistance) FindKeyLevels(List<Candle> candles, int lookback)
    {
        if (candles.Count == 0)
            return (0, 0);

        var recent = candles
            .TakeLast(Math.Min(lookback, candles.Count))
            .ToList();

        var support = recent.Min(c => c.Low);
        var resistance = recent.Max(c => c.High);

        return (support, resistance);
    }

    private string DetermineDirection(decimal close, decimal ema20, decimal ema50, decimal rsi, decimal support, decimal resistance)
    {
        var range = resistance - support;

        if (range <= 0)
            return nameof(TradeDirection.None);

        var bullish = close > ema20
                      && ema20 > ema50
                      && rsi > (decimal)_cfg.RSI_OS
                      && rsi < (decimal)_cfg.RSI_OB
                      && close > support + range * 0.3m;

        var bearish = close < ema20
                      && ema20 < ema50
                      && rsi < (decimal)_cfg.RSI_OB
                      && rsi > (decimal)_cfg.RSI_OS
                      && close < resistance - range * 0.3m;

        if (bullish)
            return nameof(TradeDirection.Long);

        if (bearish)
            return nameof(TradeDirection.Short);

        return nameof(TradeDirection.None);
    }

    private static string BuildReasoning(string pair, string direction, decimal close, decimal ema20, decimal ema50, decimal rsi, decimal atr, decimal riskReward)
    {
        var trendText = direction == nameof(TradeDirection.Long)
            ? $"Close={close:F5} is above EMA20={ema20:F5}, with EMA20 above EMA50={ema50:F5}."
            : $"Close={close:F5} is below EMA20={ema20:F5}, with EMA20 below EMA50={ema50:F5}.";

        return $"{pair} {direction} setup on Weekly timeframe. " +
               $"{trendText} " +
               $"RSI={rsi:F2}, ATR={atr:F5}, RiskReward={riskReward:F2}.";
    }

    private PairTradingConfig? GetPairConfig(string pair)
    {
        // Fix: Check if TradingSystem is null or if it has a Pairs dictionary
        if (_cfg?.TradingSystem is null)
            return null;

        // Fix: Cast TradingSystem to the correct type or access the Pairs dictionary properly
        if (_cfg.TradingSystem is not LiveTradingConfig liveConfig || liveConfig.Pairs is null || liveConfig.Pairs.Count == 0)
            return null;

        var normalizedPair = NormalizePair(pair);

        return liveConfig.Pairs
            .FirstOrDefault(p => NormalizePair(p.Key) == normalizedPair)
            .Value;
    }

    private static string NormalizePair(string pair)
    {
        return pair
            .Replace("/", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Trim()
            .ToUpperInvariant();
    }

    private static decimal RoundPrice(decimal value)
    {
        return Math.Round(value, 5);
    }
}

