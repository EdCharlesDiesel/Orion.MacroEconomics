using Skender.Stock.Indicators;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Helpers;

public class TechnicalAnalyzer
{
    private const int RsiWindow   = 14;
    private const int MacdFast    = 12;
    private const int MacdSlow    = 26;
    private const int MacdSignal  = 9;
    private const int EmaShort    = 20;
    private const int EmaLong     = 50;
    private const int BbWindow    = 20;
    private const int AtrWindow   = 14;
    private const int StochWindow = 14;
    private const int StochSmooth = 3;
    private const int AdxWindow   = 14;
    private const int SrWindow    = 20;

    /// <summary>
    /// Joins all indicator results onto each candle.
    /// Requires at least EmaLong (50) bars; returns empty list if not enough data.
    /// </summary>
    public List<IndicatorResult> AddIndicators(IReadOnlyList<Candle> candles)
    {
        if (candles.Count < EmaLong)
            return [];

        // ── Compute each indicator series ─────────────────────────────────────
        var rsiResults   = candles.GetRsi(RsiWindow).ToArray();
        var macdResults  = candles.GetMacd(MacdFast, MacdSlow, MacdSignal).ToArray();
        var ema20Results = candles.GetEma(EmaShort).ToArray();
        var ema50Results = candles.GetEma(EmaLong).ToArray();
        var bbResults    = candles.GetBollingerBands(BbWindow, 2).ToArray();
        var atrResults   = candles.GetAtr(AtrWindow).ToArray();
        var stochResults = candles.GetStoch(StochWindow, StochSmooth).ToArray();
        var adxResults   = candles.GetAdx(AdxWindow).ToArray();

        // ── Rolling resistance / support over SrWindow bars ───────────────────
        var highs = candles.Select(c => c.High).ToArray();
        var lows  = candles.Select(c => c.Low).ToArray();

        var results = new List<IndicatorResult>(candles.Count);

        for (int i = 0; i < candles.Count; i++)
        {
            var c   = candles[i];
            var sr  = i >= SrWindow - 1
                ? new { R = highs[(i - SrWindow + 1)..(i + 1)].Max(),
                        S = lows[ (i - SrWindow + 1)..(i + 1)].Min() }
                : null;

            results.Add(new IndicatorResult
            {
                Date  = c.Date,
                Open  = c.Open,
                High  = c.High,
                Low   = c.Low,
                Close = c.Close,

                RSI           = i < rsiResults.Length   ? ToDecimal(rsiResults[i].Rsi)        : null,
                MACD          = i < macdResults.Length  ? ToDecimal(macdResults[i].Macd)      : null,
                MACDSignal    = i < macdResults.Length  ? ToDecimal(macdResults[i].Signal)    : null,
                MACDHistogram = i < macdResults.Length  ? ToDecimal(macdResults[i].Histogram) : null,
                EMA20         = i < ema20Results.Length ? ToDecimal(ema20Results[i].Ema)      : null,
                EMA50         = i < ema50Results.Length ? ToDecimal(ema50Results[i].Ema)      : null,
                BBUpper       = i < bbResults.Length    ? ToDecimal(bbResults[i].UpperBand)   : null,
                BBMiddle      = i < bbResults.Length    ? ToDecimal(bbResults[i].Sma)         : null,
                BBLower       = i < bbResults.Length    ? ToDecimal(bbResults[i].LowerBand)   : null,
                ATR           = i < atrResults.Length   ? ToDecimal(atrResults[i].Atr)        : null,
                StochK        = i < stochResults.Length ? ToDecimal(stochResults[i].K)        : null,
                StochD        = i < stochResults.Length ? ToDecimal(stochResults[i].D)        : null,
                ADX           = i < adxResults.Length   ? ToDecimal(adxResults[i].Adx)        : null,
                ADXPos        = i < adxResults.Length   ? ToDecimal(adxResults[i].Pdi)        : null,
                ADXNeg        = i < adxResults.Length   ? ToDecimal(adxResults[i].Mdi)        : null,
                Resistance20  = sr?.R,
                Support20     = sr?.S,
            });
        }

        return results;
    }

    /// <summary>Standard pivot points based on the second-to-last candle.</summary>
    public PivotLevels CalculatePivots(IReadOnlyList<Candle> candles)
    {
        var ref_ = candles.Count >= 2 ? candles[^2] : candles[^1];
        decimal h = ref_.High, l = ref_.Low, c = ref_.Close;
        decimal pp = (h + l + c) / 3m;
        decimal r1 = 2m * pp - l, r2 = pp + (h - l), r3 = h + 2m * (pp - l);
        decimal s1 = 2m * pp - h, s2 = pp - (h - l), s3 = l - 2m * (h - pp);
        return new PivotLevels
        {
            PP = pp,
            R1 = r1, R2 = r2, R3 = r3,
            S1 = s1, S2 = s2, S3 = s3,
            M1 = (s1 + pp) / 2m,  M2 = (pp + r1) / 2m,
            M3 = (r1 + r2) / 2m,  M4 = (r2 + r3) / 2m,
            M0 = (s1 + s2) / 2m,
        };
    }

    /// <summary>Fibonacci retracement levels over the last <paramref name="lookback"/> candles.</summary>
    public Dictionary<string, decimal> CalculateFibonacci(IReadOnlyList<Candle> candles, int lookback = 12)
    {
        var slice = candles.TakeLast(lookback).ToList();
        decimal hi   = slice.Max(c => c.High);
        decimal lo   = slice.Min(c => c.Low);
        decimal diff = hi - lo;
        return new()
        {
            ["0.0%"]   = hi,
            ["23.6%"]  = hi - 0.236m * diff,
            ["38.2%"]  = hi - 0.382m * diff,
            ["50.0%"]  = hi - 0.500m * diff,
            ["61.8%"]  = hi - 0.618m * diff,
            ["78.6%"]  = hi - 0.786m * diff,
            ["100.0%"] = lo,
        };
    }

    /// <summary>Bullish / Bearish / Neutral based on EMA20 and RSI.</summary>
    public string GetSentiment(IReadOnlyList<IndicatorResult> indicators)
    {
        if (indicators.Count == 0) return "Neutral";
        var last = indicators[^1];
        if (last.EMA20 is null) return "Neutral";
        decimal price = last.Close;
        decimal ema20 = last.EMA20.Value;
        decimal rsi   = last.RSI ?? 50m;
        if (price > ema20 && rsi > 50m) return "Bullish";
        if (price < ema20 && rsi < 50m) return "Bearish";
        return "Neutral";
    }

    /// <summary>
    /// Generates QuantConnect-style BUY / SELL / STRONG BUY / STRONG SELL signals
    /// based on pivot position, MA alignment and RSI.
    /// </summary>
    public List<IndicatorResult> GenerateProSignals(List<IndicatorResult> indicators, PivotLevels pivots)
    {
        if (indicators.Count < 10) return indicators;

        // Compute short-term MAs inline
        decimal[] closes = indicators.Select(r => r.Close).ToArray();
        decimal? MA(int i, int window) => i >= window - 1
            ? closes[(i - window + 1)..(i + 1)].Average()
            : null;

        for (int i = 0; i < indicators.Count; i++)
        {
            var r    = indicators[i];
            decimal  c    = r.Close;
            decimal? ma5  = MA(i, 5);
            decimal? ma10 = MA(i, 10);
            decimal? ma20 = r.EMA20;
            decimal? ma50 = r.EMA50;
            decimal  rsi  = r.RSI ?? 50m;

            decimal pp = pivots.PP;
            decimal r1 = pivots.R1;
            decimal s1 = pivots.S1;

            bool maAlignLong  = ma5.HasValue && ma10.HasValue && ma20.HasValue
                             && ma5 > ma10 && ma10 > ma20 && c > ma20;
            bool maAlignShort = ma5.HasValue && ma10.HasValue && ma20.HasValue
                             && ma5 < ma10 && ma10 < ma20 && c < ma20;

            bool buy   = c > pp && c < r1 && maAlignLong  && rsi < 70m;
            bool sell  = c < pp && c > s1 && maAlignShort && rsi > 30m;
            bool sBuy  = buy  && ma50.HasValue && c > ma50 && rsi < 60m;
            bool sSell = sell && ma50.HasValue && c < ma50 && rsi > 40m;

            r.Signal = sBuy  ? "STRONG BUY"
                     : buy   ? "BUY"
                     : sSell ? "STRONG SELL"
                     : sell  ? "SELL"
                              : "HOLD";
            r.SignalScore = r.Signal switch
            {
                "STRONG BUY"  =>  2,
                "BUY"         =>  1,
                "SELL"        => -1,
                "STRONG SELL" => -2,
                _             =>  0,
            };
        }
        return indicators;
    }

    private static decimal? ToDecimal(double? value) =>
        value.HasValue ? (decimal)value.Value : null;
}
