using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configurations;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Extensions;

public sealed class TradePlanFactory(IOptions<AppConfiguration> config, ILogger<TradePlanFactory> logger)
{
    private readonly AppConfiguration _cfg = config?.Value ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<TradePlanFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private int MinimumCandles =>
        Math.Max(_cfg.Indicators.LongEmaPeriod, Math.Max(_cfg.Indicators.AtrPeriod + 1, _cfg.Indicators.RsiPeriod + 1));

    public TradePlan? CreateFromCandles(string pair, List<Candle> candles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        ArgumentNullException.ThrowIfNull(candles);

        var minCandles = MinimumCandles;

        if (candles.Count < minCandles)
        {
            _logger.LogWarning(
                "Not enough candles for {Pair}. Need {Required}, have {Count}",
                pair, minCandles, candles.Count);
            return null;
        }

        var sorted = candles
            .Where(c => c.High > 0 && c.Low > 0 && c.Close > 0)
            .OrderBy(c => c.Time)
            .ToList();

        if (sorted.Count < minCandles)
        {
            _logger.LogWarning(
                "Not enough valid candles for {Pair}. Need {Required}, have {Count}",
                pair, minCandles, sorted.Count);
            return null;
        }

        var ind = _cfg.Indicators;

        var atr        = CalculateAtr(sorted, ind.AtrPeriod);
        var ema20      = CalculateEma(sorted, ind.ShortEmaPeriod);
        var ema50      = CalculateEma(sorted, ind.LongEmaPeriod);
        var rsi        = CalculateRsi(sorted, ind.RsiPeriod);
        var (support, resistance) = FindKeyLevels(sorted, ind.KeyLevelLookback);

        if (atr <= 0)
        {
            _logger.LogWarning("{Pair}: ATR is zero or invalid. Skipping.", pair);
            return null;
        }

        if (support <= 0 || resistance <= 0 || resistance <= support)
        {
            _logger.LogWarning(
                "{Pair}: invalid key levels. Support={Support}, Resistance={Resistance}",
                pair, support, resistance);
            return null;
        }

        var close = (decimal)sorted[^1].Close;

        _logger.LogInformation(
            "{Pair} | Close={Close} EMA20={EMA20:F5} EMA50={EMA50:F5} ATR={ATR:F5} RSI={RSI:F2} Support={Support:F5} Resistance={Resistance:F5}",
            pair, close, ema20, ema50, atr, rsi, support, resistance);

        var direction = DetermineDirection(close, ema20, ema50, rsi, (decimal)support, (decimal)resistance);

        if (direction == TradeDirection.None)
        {
            _logger.LogInformation("{Pair}: no valid directional signal. Skipping.", pair);
            return null;
        }

        var risk         = _cfg.Risk;
        var pairCfg      = GetPairConfig(pair);

        var atrStopMultiplier = pairCfg?.AtrStopMultiplier ?? risk.AtrStopMultiplier;
        var minStopDistance   = pairCfg?.MinStopDistance   ?? risk.DefaultMinStop;

        var stopDistance = Math.Max((decimal)atr * atrStopMultiplier, minStopDistance);

        var isLong = direction == TradeDirection.Long;

        var entry       = close;
        var stopLoss    = isLong ? entry - stopDistance              : entry + stopDistance;
        var takeProfit1 = isLong ? entry + stopDistance * risk.Tp1AtrMultiplier : entry - stopDistance * risk.Tp1AtrMultiplier;
        var takeProfit2 = isLong ? entry + stopDistance * risk.Tp2AtrMultiplier : entry - stopDistance * risk.Tp2AtrMultiplier;

        var riskReward = Math.Abs(takeProfit1 - entry) / stopDistance;

        if (riskReward < risk.MinRiskReward)
        {
            _logger.LogInformation(
                "{Pair}: R:R {RiskReward:F2} is below minimum {MinRR:F2}. Skipping.",
                pair, riskReward, risk.MinRiskReward);
            return null;
        }

        var plan = new TradePlan
        {
            Id        = Guid.NewGuid(),
            Pair      = pair,
            Direction = direction.ToString(),
            Status    = TradePlanStatus.Pending.ToString(),
            OpenedAt  = DateTime.UtcNow,

            EntryPrice   = RoundPrice(entry),
            StopLoss     = RoundPrice(stopLoss),
            TakeProfit1  = RoundPrice(takeProfit1),
            TakeProfit2  = RoundPrice(takeProfit2),
            RiskReward   = Math.Round(riskReward, 2),

            ATR        = Math.Round(atr, 5),
            EMA20      = Math.Round(ema20, 5),
            EMA50      = Math.Round(ema50, 5),
            RSI        = Math.Round(rsi, 2),
            Support    = Math.Round(support, 5),
            Resistance = Math.Round(resistance, 5),

            Timeframe = "Weekly",
            Reasoning = BuildReasoning(pair, direction, close, ema20, ema50, rsi, atr, riskReward),
        };

        _logger.LogInformation(
            "TradePlan created for {Pair} | Direction={Direction} Entry={Entry} SL={SL} TP1={TP1} TP2={TP2} RR={RR:F2}",
            pair, direction, plan.EntryPrice, plan.StopLoss, plan.TakeProfit1, plan.TakeProfit2, plan.RiskReward);

        return plan;
    }

    private static decimal CalculateAtr(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
            return 0;

        var trueRanges = new decimal[candles.Count - 1];

        for (var i = 1; i < candles.Count; i++)
        {
            var high  = candles[i].High;
            var low   = candles[i].Low;
            var prevClose = candles[i - 1].Close;

            trueRanges[i - 1] = Math.Max(
                high - low,
                Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
        }

        return trueRanges.TakeLast(period).Average();
    }

    private static decimal CalculateEma(List<Candle> candles, int period)
    {
        if (candles.Count < period)
            return 0;

        var multiplier = 2m / (period + 1);
        var ema = (decimal)candles.Take(period).Average(c => c.Close);

        foreach (var candle in candles.Skip(period))
            ema = ((decimal)candle.Close - ema) * multiplier + ema;

        return ema;
    }

    private static decimal CalculateRsi(List<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
            return 50;

        var recent = candles.TakeLast(period + 1).ToList();

        var gains  = 0m;
        var losses = 0m;

        for (var i = 1; i < recent.Count; i++)
        {
            var change = recent[i].Close - recent[i - 1].Close;
            if (change > 0) gains  += change;
            else            losses += Math.Abs(change);
        }

        var avgGain = gains  / period;
        var avgLoss = losses / period;

        if (avgLoss == 0)
            return 100;

        var rs = avgGain / avgLoss;
        return 100 - 100 / (1 + rs);
    }

    private static (decimal Support, decimal Resistance) FindKeyLevels(List<Candle> candles, int lookback)
    {
        if (candles.Count == 0)
            return (0, 0);

        var recent = candles.TakeLast(Math.Min(lookback, candles.Count));
        return (recent.Min(c => c.Low), recent.Max(c => c.High));
    }

    private TradeDirection DetermineDirection(decimal close, decimal ema20, decimal ema50, decimal rsi, decimal support, decimal resistance)
    {
        var range = resistance - support;
        if (range <= 0)
            return TradeDirection.None;

        var risk = _cfg.Risk;

        var bullish = close > ema20
                   && ema20  > ema50
                   && rsi    > risk.RsiOversold
                   && rsi    < risk.RsiOverbought
                   && close  > support + range * 0.3m;

        var bearish = close < ema20
                   && ema20  < ema50
                   && rsi    < risk.RsiOverbought
                   && rsi    > risk.RsiOversold
                   && close  < resistance - range * 0.3m;

        if (bullish) return TradeDirection.Long;
        if (bearish) return TradeDirection.Short;

        return TradeDirection.None;
    }

    private static string BuildReasoning(string pair, TradeDirection direction, decimal close, decimal ema20, decimal ema50, decimal rsi, decimal atr, decimal riskReward)
    {
        var trend = direction == TradeDirection.Long
            ? $"Close={close:F5} above EMA20={ema20:F5}, EMA20 above EMA50={ema50:F5}."
            : $"Close={close:F5} below EMA20={ema20:F5}, EMA20 below EMA50={ema50:F5}.";

        return $"{pair} {direction} setup on Weekly timeframe. {trend} RSI={rsi:F2}, ATR={atr:F5}, R:R={riskReward:F2}.";
    }

    private PairTradingConfig? GetPairConfig(string pair)
    {
        var pairs = _cfg.LiveTrading?.Pairs;
        if (pairs is null || pairs.Count == 0)
            return null;

        var normalized = NormalizePair(pair);

        return pairs.TryGetValue(normalized, out var exact)
            ? exact
            : pairs.FirstOrDefault(kv => NormalizePair(kv.Key) == normalized).Value;
    }

    private static string NormalizePair(string pair)
    {
        return pair.Replace("/", "").Replace("-", "").Replace("_", "").Trim().ToUpperInvariant();
    }

    private static decimal RoundPrice(decimal value) => Math.Round(value, 5);
}