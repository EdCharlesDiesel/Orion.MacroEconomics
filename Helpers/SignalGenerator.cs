using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configurations;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers;

public class SignalGenerator(IOptions<AppSettings> opts) // ✅ removed unused TechnicalAnalyzer
{
    private readonly RiskSettings _risk = opts.Value.Risk;

    // ✅ Use constants instead of magic strings
    private const string Long  = "Long";
    private const string Short = "Short";

    public (int Signal, int Confidence, List<string> Reasons) GetEntrySignal(
        List<IndicatorResult> indicators15m, string bias)
    {
        if (indicators15m.Count < 5)
            return (0, 0, ["Insufficient 15-min data"]);

        var last = indicators15m[^1];
        var prev = indicators15m[^2];

        decimal k     = last.StochK ?? 50m, d     = last.StochD ?? 50m;
        decimal prevK = prev.StochK ?? 50m, prevD = prev.StochD ?? 50m;
        decimal rsi   = last.RSI    ?? 50m;
        decimal price = last.Close;
        if (price <= 0) return (0, 0, ["Invalid price"]);

        decimal bbLo = last.BBLower ?? price * 0.99m;
        decimal bbHi = last.BBUpper ?? price * 1.01m;

        int signal = 0, confidence = 0;
        var reasons = new List<string>();

        if (bias == Long)
        {
            if (prevK <= prevD && d < k && k < _risk.StochOversold)
            {
                signal = 1;
                confidence += 2;
                reasons.Add($"Stoch bullish crossover (K={k:F1})");
            }
            if (rsi < _risk.RsiOversold)
            {
                confidence++;
                reasons.Add($"RSI oversold ({rsi:F1})");
            }
            if (price <= bbLo * 1.002m)
            {
                confidence++;
                reasons.Add("Price at lower Bollinger Band");
            }
            if (reasons.Count == 0)
                reasons.Add($"Awaiting Long trigger — K={k:F1}, RSI={rsi:F1}");
        }
        else if (bias == Short)
        {
            if (prevK >= prevD && d > k && k > _risk.StochOverbought)
            {
                signal = -1;
                confidence += 2;
                reasons.Add($"Stoch bearish crossover (K={k:F1})");
            }
            if (rsi > _risk.RsiOverbought)
            {
                confidence++;
                reasons.Add($"RSI overbought ({rsi:F1})");
            }
            if (price >= bbHi * 0.998m)
            {
                confidence++;
                reasons.Add("Price at upper Bollinger Band");
            }
            if (reasons.Count == 0)
                reasons.Add($"Awaiting Short trigger — K={k:F1}, RSI={rsi:F1}");
        }
        else
        {
            reasons.Add($"Trend bias is Neutral (ADX < {_risk.AdxTrendMin})");
        }

        return (signal, Math.Min(confidence, 5), reasons);
    }

    public TradeSignal? AnalyzeMultiTimeframe(
        List<IndicatorResult> daily, List<IndicatorResult> h4,
        List<IndicatorResult> h1,   List<IndicatorResult> m15,
        string pairName)
    {
        if (daily.Count == 0 || h4.Count == 0 || h1.Count == 0 || m15.Count == 0)
            return null;

        var dLast  = daily[^1];
        var h4Last = h4[^1];
        var h1Last = h1[^1];

        decimal dClose  = dLast.Close;
        decimal dEma20  = dLast.EMA20 ?? dClose;
        string  dTrend  = dClose > dEma20 ? Long : Short;
        decimal dRsi    = dLast.RSI ?? 50m;
        decimal dAdx    = dLast.ADX ?? 0m;

        decimal h4Close  = h4Last.Close;
        decimal h4Ema20  = h4Last.EMA20 ?? h4Close;
        decimal h4Ema50  = h4Last.EMA50 ?? h4Close;
        string  h4Trend  = h4Ema20 > h4Ema50 ? Long : Short;
        decimal h4Macd   = h4Last.MACD       ?? 0m;
        decimal h4Sig    = h4Last.MACDSignal ?? 0m;

        decimal h1Close = h1Last.Close;
        decimal h1Ema20 = h1Last.EMA20 ?? h1Close;
        decimal h1Ema50 = h1Last.EMA50 ?? h1Close;
        string  h1Trend = h1Ema20 > h1Ema50 ? Long : Short;
        decimal h1Rsi   = h1Last.RSI ?? 50m;

        int longScore = 0, shortScore = 0;
        var reasons = new List<string>();

        void Score(bool condition, string bull, string bear)
        {
            if (condition) { longScore++;  reasons.Add(bull); }
            else           { shortScore++; reasons.Add(bear); }
        }

        // Daily
        if (dTrend == Long) { longScore  += 2; reasons.Add("Daily: Bullish EMA alignment"); }
        else                { shortScore += 2; reasons.Add("Daily: Bearish EMA alignment"); }

        if      (dRsi < 40m) { longScore++;  reasons.Add($"Daily RSI oversold ({dRsi:F1})");  }
        else if (dRsi > 60m) { shortScore++; reasons.Add($"Daily RSI overbought ({dRsi:F1})"); }

        if (dAdx > _risk.AdxTrendMin)
        {
            if (dTrend == Long) longScore++; else shortScore++;
            reasons.Add($"Strong trend (ADX={dAdx:F1})");
        }

        Score(h4Trend == Long,    "4H: EMA20 > EMA50",  "4H: EMA20 < EMA50");
        Score(h4Macd  > h4Sig,   "4H: MACD bullish",   "4H: MACD bearish");
        Score(h1Trend == Long,    "1H: Bullish EMA",    "1H: Bearish EMA");

        if      (h1Rsi < 45m) { longScore++;  reasons.Add($"1H RSI supportive ({h1Rsi:F1})"); }
        else if (h1Rsi > 55m) { shortScore++; reasons.Add($"1H RSI resistive ({h1Rsi:F1})");  }

        if (longScore == shortScore) return null;

        string finalBias = longScore > shortScore ? Long : Short;
        int    strength  = Math.Max(longScore, shortScore);
        int    normScore = Math.Min((int)(strength * 1.25m), 10);
        string conviction = strength >= 6 ? "High" : strength >= 3 ? "Medium" : "Low";

        var (entrySignal, entryConf, entryReasons) = GetEntrySignal(m15, finalBias);

        // ✅ Only proceed if there is an actual entry trigger
        if (entrySignal == 0)
            return null;

        // ✅ Blend multi-timeframe strength with entry confidence
        int confidence = Math.Min((normScore + entryConf * 2) / 2, 10);

        decimal atr     = h1Last.ATR ?? h1Close * 0.005m;
        decimal price15 = m15[^1].Close;
        if (price15 <= 0) return null;

        var sl = CalculateSl(h1, pairName, finalBias, price15, atr);
        var tp = CalculateTp(h4, finalBias, price15, atr, sl.Stop); // ✅ removed unused pair arg

        string thesis = string.Join(" | ", reasons);
        if (entryReasons.Count > 0)
            thesis += " | Entry: " + string.Join(", ", entryReasons.Take(2));

        return new TradeSignal
        {
            Pair            = pairName,
            Bias            = finalBias,
            Conviction      = conviction,
            StrengthScore   = normScore,
            Confidence      = confidence,
            Thesis          = thesis,
            Entry           = price15,
            TakeProfit1     = tp.Tp1,
            TakeProfit2     = tp.Tp2,
            StopLoss        = sl.Stop,
            RiskReward1     = tp.RR1,
            RiskReward2     = tp.RR2,
            StopLossMethod  = sl.Method,
            StopLossPips    = sl.Pips,
            ATR             = atr,
            GeneratedAt     = DateTime.UtcNow,
        };
    }

    private (decimal Stop, string Method, decimal Pips) CalculateSl(
        List<IndicatorResult> h1, string pair, string bias, decimal price, decimal atr)
    {
        decimal mult   = AppConfig.PairAtrMultipliers.GetValueOrDefault(pair, _risk.AtrSlMultiplier);
        decimal minDist = AppConfig.PairMinStop.GetValueOrDefault(pair, 0.001m);
        decimal buf    = atr * 0.25m;

        decimal stop   = bias == Long ? price - atr * mult : price + atr * mult;
        string  method = "ATR";

        if (h1.Count >= 20)
        {
            var slice = h1.TakeLast(20).ToList();

            if (bias == Long)
            {
                decimal swing = slice.Min(r => r.Low) - buf;
                // ✅ Use swing low if it's a valid level below price (tighter or wider — it's the real S/R)
                if (swing < price)
                {
                    stop   = swing;
                    method = "Swing Low";
                }
            }
            else
            {
                decimal swing = slice.Max(r => r.High) + buf;
                // ✅ Use swing high if it's a valid level above price
                if (swing > price)
                {
                    stop   = swing;
                    method = "Swing High";
                }
            }
        }

        if (Math.Abs(price - stop) < minDist)
        {
            stop    = bias == Long ? price - minDist : price + minDist;
            method += " + min-dist";
        }

        decimal pips = Math.Abs(price - stop) / AppConfig.PipSize(pair);
        return (stop, method, Math.Round(pips, 1));
    }

    // ✅ Removed unused `pair` parameter
    private (decimal Tp1, decimal Tp2, decimal RR1, decimal RR2) CalculateTp(
        List<IndicatorResult> h4, string bias, decimal price, decimal atr, decimal sl)
    {
        decimal stopDist = Math.Abs(price - sl);
        if (stopDist == 0) stopDist = atr;

        decimal tp1, tp2;
        if (bias == Long)
        {
            tp1 = price + atr * _risk.Tp1AtrMultiplier;
            tp2 = price + atr * _risk.Tp2AtrMultiplier;

            if (h4.Count >= 20)
            {
                decimal swingHi = h4.TakeLast(20).Max(r => r.High);
                if (tp1 < swingHi && swingHi < tp2) tp2 = swingHi;
            }
        }
        else
        {
            tp1 = price - atr * _risk.Tp1AtrMultiplier;
            tp2 = price - atr * _risk.Tp2AtrMultiplier;

            if (h4.Count >= 20)
            {
                decimal swingLo = h4.TakeLast(20).Min(r => r.Low);
                if (tp2 < swingLo && swingLo < tp1) tp2 = swingLo;
            }
        }

        decimal rr1 = bias == Long ? (tp1 - price) / stopDist : (price - tp1) / stopDist;
        decimal rr2 = bias == Long ? (tp2 - price) / stopDist : (price - tp2) / stopDist;

        return (tp1, tp2, Math.Round(rr1, 2), Math.Round(rr2, 2));
    }
}