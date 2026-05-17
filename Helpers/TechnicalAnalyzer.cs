// using Orion.MacroEconomics.DTO;
// using Orion.MacroEconomics.Entities;
//
// namespace Orion.MacroEconomics.Helpers;
//
// public class TechnicalAnalyzer
// {
//     private const int RsiWindow   = 14;
//     private const int MacdFast    = 12;
//     private const int MacdSlow    = 26;
//     private const int MacdSignal  = 9;
//     private const int EmaShort    = 20;
//     private const int EmaLong     = 50;
//     private const int BbWindow    = 20;
//     private const int AtrWindow   = 14;
//     private const int StochWindow = 14;
//     private const int StochSmooth = 3;
//     private const int AdxWindow   = 14;
//     private const int SrWindow    = 20;
//
//     /// <summary>
//     /// Joins all indicator results onto each candle.
//     /// Requires at least EmaLong (50) bars; returns empty list if not enough data.
//     /// </summary>
//     public List<IndicatorResult> AddIndicators(IReadOnlyList<Candle> candles)
//     {
//         if (candles.Count < EmaLong)
//             return [];
//
//         // ── Compute each indicator series ─────────────────────────────────────
//         var rsiResults   = candles.GetRsi(RsiWindow).ToArray();
//         var macdResults  = candles.GetMacd(MacdFast, MacdSlow, MacdSignal).ToArray();
//         var ema20Results = candles.GetEma(EmaShort).ToArray();
//         var ema50Results = candles.GetEma(EmaLong).ToArray();
//         var bbResults    = candles.GetBollingerBands(BbWindow, 2).ToArray();
//         var atrResults   = candles.GetAtr(AtrWindow).ToArray();
//         var stochResults = candles.GetStoch(StochWindow, StochSmooth).ToArray();
//         var adxResults   = candles.GetAdx(AdxWindow).ToArray();
//
//         // ── Rolling resistance / support over SrWindow bars ───────────────────
//         var highs = candles.Select(c => (double)c.High).ToArray();
//         var lows  = candles.Select(c => (double)c.Low).ToArray();
//
//         var results = new List<IndicatorResult>(candles.Count);
//
//         for (int i = 0; i < candles.Count; i++)
//         {
//             var c   = candles[i];
//             var sr  = i >= SrWindow - 1
//                 ? new { R = highs[(i - SrWindow + 1)..(i + 1)].Max(),
//                         S = lows[ (i - SrWindow + 1)..(i + 1)].Min() }
//                 : null;
//
//             results.Add(new IndicatorResult
//             {
//                 Date  = c.Date,
//                 Open  = c.Open,
//                 High  = c.High,
//                 Low   = c.Low,
//                 Close = c.Close,
//
//                 RSI           = i < rsiResults.Length   ? rsiResults[i].Rsi        : null,
//                 MACD          = i < macdResults.Length  ? macdResults[i].Macd       : null,
//                 MACDSignal    = i < macdResults.Length  ? macdResults[i].Signal     : null,
//                 MACDHistogram = i < macdResults.Length  ? macdResults[i].Histogram  : null,
//                 EMA20         = i < ema20Results.Length ? ema20Results[i].Ema       : null,
//                 EMA50         = i < ema50Results.Length ? ema50Results[i].Ema       : null,
//                 BBUpper       = i < bbResults.Length    ? bbResults[i].UpperBand    : null,
//                 BBMiddle      = i < bbResults.Length    ? bbResults[i].Sma          : null,
//                 BBLower       = i < bbResults.Length    ? bbResults[i].LowerBand    : null,
//                 ATR           = i < atrResults.Length   ? atrResults[i].Atr        : null,
//                 StochK        = i < stochResults.Length ? stochResults[i].K         : null,
//                 StochD        = i < stochResults.Length ? stochResults[i].D         : null,
//                 ADX           = i < adxResults.Length   ? adxResults[i].Adx        : null,
//                 ADXPos        = i < adxResults.Length   ? adxResults[i].Pdi        : null,
//                 ADXNeg        = i < adxResults.Length   ? adxResults[i].Mdi        : null,
//                 Resistance20  = sr?.R,
//                 Support20     = sr?.S,
//             });
//         }
//
//         return results;
//     }
//
//     /// <summary>Standard pivot points based on the second-to-last candle.</summary>
//     public PivotLevels CalculatePivots(IReadOnlyList<Candle> candles)
//     {
//         var ref_ = candles.Count >= 2 ? candles[^2] : candles[^1];
//         double h = (double)ref_.High, l = (double)ref_.Low, c = (double)ref_.Close;
//         double pp = (h + l + c) / 3;
//         double r1 = 2 * pp - l, r2 = pp + (h - l), r3 = h + 2 * (pp - l);
//         double s1 = 2 * pp - h, s2 = pp - (h - l), s3 = l - 2 * (h - pp);
//         return new PivotLevels
//         {
//             PP = pp,
//             R1 = r1, R2 = r2, R3 = r3,
//             S1 = s1, S2 = s2, S3 = s3,
//             M1 = (s1 + pp) / 2,  M2 = (pp + r1) / 2,
//             M3 = (r1 + r2) / 2,  M4 = (r2 + r3) / 2,
//             M0 = (s1 + s2) / 2,
//         };
//     }
//
//     /// <summary>Fibonacci retracement levels over the last <paramref name="lookback"/> candles.</summary>
//     public Dictionary<string, double> CalculateFibonacci(IReadOnlyList<Candle> candles, int lookback = 12)
//     {
//         var slice = candles.TakeLast(lookback).ToList();
//         double hi = (double)slice.Max(c => c.High);
//         double lo = (double)slice.Min(c => c.Low);
//         double diff = hi - lo;
//         return new()
//         {
//             ["0.0%"]   = hi,
//             ["23.6%"]  = hi - 0.236 * diff,
//             ["38.2%"]  = hi - 0.382 * diff,
//             ["50.0%"]  = hi - 0.500 * diff,
//             ["61.8%"]  = hi - 0.618 * diff,
//             ["78.6%"]  = hi - 0.786 * diff,
//             ["100.0%"] = lo,
//         };
//     }
//
//     /// <summary>Bullish / Bearish / Neutral based on EMA20 and RSI.</summary>
//     public string GetSentiment(IReadOnlyList<IndicatorResult> indicators)
//     {
//         if (indicators.Count == 0) return "Neutral";
//         var last = indicators[^1];
//         if (last.EMA20 is null) return "Neutral";
//         double price = (double)last.Close;
//         double ema20 = last.EMA20.Value;
//         double rsi   = last.RSI ?? 50.0;
//         if (price > ema20 && rsi > 50) return "Bullish";
//         if (price < ema20 && rsi < 50) return "Bearish";
//         return "Neutral";
//     }
//
//     /// <summary>
//     /// Generates QuantConnect-style BUY / SELL / STRONG BUY / STRONG SELL signals
//     /// based on pivot position, MA alignment and RSI.
//     /// </summary>
//     public List<IndicatorResult> GenerateProSignals(List<IndicatorResult> indicators, PivotLevels pivots)
//     {
//         if (indicators.Count < 10) return indicators;
//
//         // Compute short-term MAs inline
//         double[] closes = indicators.Select(r => (double)r.Close).ToArray();
//         double MA(int i, int window) => i >= window - 1
//             ? closes[(i - window + 1)..(i + 1)].Average()
//             : double.NaN;
//
//         for (int i = 0; i < indicators.Count; i++)
//         {
//             var r    = indicators[i];
//             double c = (double)r.Close;
//             double ma5  = MA(i, 5),  ma10 = MA(i, 10);
//             double ma20 = r.EMA20 ?? double.NaN;
//             double ma50 = r.EMA50 ?? double.NaN;
//             double rsi  = r.RSI   ?? 50.0;
//
//             bool buy  = c > pivots.PP && c < pivots.R1 && ma5 > ma10 && ma10 > ma20 && rsi < 70 && c > ma20;
//             bool sell = c < pivots.PP && c > pivots.S1 && ma5 < ma10 && ma10 < ma20 && rsi > 30 && c < ma20;
//             bool sBuy  = buy  && c > ma50 && rsi < 60;
//             bool sSell = sell && c < ma50 && rsi > 40;
//
//             r.Signal = sBuy  ? "STRONG BUY"
//                      : buy   ? "BUY"
//                      : sSell ? "STRONG SELL"
//                      : sell  ? "SELL"
//                               : "HOLD";
//             r.SignalScore = r.Signal switch
//             {
//                 "STRONG BUY"  =>  2,
//                 "BUY"         =>  1,
//                 "SELL"        => -1,
//                 "STRONG SELL" => -2,
//                 _             =>  0,
//             };
//         }
//         return indicators;
//     }
// }
