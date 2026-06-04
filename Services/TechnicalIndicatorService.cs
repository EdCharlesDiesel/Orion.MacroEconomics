using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;



public class TechnicalIndicatorService
{
    public List<double> CalculateEma(List<double> prices, int period)
    {
        var ema = new List<double>();
        if (prices.Count < period) return ema;

        double multiplier = 2.0 / (period + 1);
        double firstEma = prices.Take(period).Average();
        ema.Add(firstEma);

        for (int i = period; i < prices.Count; i++)
        {
            double value = (prices[i] - ema.Last()) * multiplier + ema.Last();
            ema.Add(value);
        }

        return ema;
    }

    public List<double> CalculateRsi(List<double> prices, int period = 14)
    {
        var rsi = new List<double>();
        if (prices.Count < period + 1) return rsi;

        var gains = new List<double>();
        var losses = new List<double>();

        for (int i = 1; i < prices.Count; i++)
        {
            double delta = prices[i] - prices[i - 1];
            gains.Add(Math.Max(delta, 0));
            losses.Add(Math.Max(-delta, 0));
        }

        double avgGain = gains.Take(period).Average();
        double avgLoss = losses.Take(period).Average();

        for (int i = period; i < gains.Count; i++)
        {
            avgGain = (avgGain * (period - 1) + gains[i]) / period;
            avgLoss = (avgLoss * (period - 1) + losses[i]) / period;

            double rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            rsi.Add(100 - (100 / (1 + rs)));
        }

        return rsi;
    }

    public (List<double> macd, List<double> signal, List<double> histogram) CalculateMacd(
        List<double> prices, int fast = 12, int slow = 26, int signalPeriod = 9)
    {
        var emaFast = CalculateEma(prices, fast);
        var emaSlow = CalculateEma(prices, slow);

        int offset = emaFast.Count - emaSlow.Count;
        var macdLine = new List<double>();

        for (int i = 0; i < emaSlow.Count; i++)
        {
            macdLine.Add(emaFast[i + offset] - emaSlow[i]);
        }

        var signalLine = CalculateEma(macdLine, signalPeriod);
        var histogram = new List<double>();

        for (int i = 0; i < signalLine.Count; i++)
        {
            histogram.Add(macdLine[i + (macdLine.Count - signalLine.Count)] - signalLine[i]);
        }

        return (macdLine, signalLine, histogram);
    }

    public (List<double> adx, List<double> plusDi, List<double> minusDi) CalculateAdx(
        List<double> high, List<double> low, List<double> close, int period = 14)
    {
        var tr = new List<double>();
        var plusDm = new List<double>();
        var minusDm = new List<double>();

        for (int i = 1; i < high.Count; i++)
        {
            double tr1 = high[i] - low[i];
            double tr2 = Math.Abs(high[i] - close[i - 1]);
            double tr3 = Math.Abs(low[i] - close[i - 1]);
            tr.Add(Math.Max(Math.Max(tr1, tr2), tr3));

            double upMove = high[i] - high[i - 1];
            double downMove = low[i - 1] - low[i];

            plusDm.Add((upMove > downMove && upMove > 0) ? upMove : 0);
            minusDm.Add((downMove > upMove && downMove > 0) ? downMove : 0);
        }

        var atrValues = CalculateEma(tr, period);
        var plusDiValues = new List<double>();
        var minusDiValues = new List<double>();

        int startIdx = tr.Count - atrValues.Count;
        for (int i = 0; i < atrValues.Count; i++)
        {
            plusDiValues.Add(atrValues[i] > 0 ? (100 * plusDm[i + startIdx] / atrValues[i]) : 0);
            minusDiValues.Add(atrValues[i] > 0 ? (100 * minusDm[i + startIdx] / atrValues[i]) : 0);
        }

        var adxValues = new List<double>();
        for (int i = 0; i < plusDiValues.Count; i++)
        {
            double sum = plusDiValues[i] + minusDiValues[i];
            adxValues.Add(sum > 0 ? (100 * Math.Abs(plusDiValues[i] - minusDiValues[i]) / sum) : 0);
        }

        var finalAdx = CalculateEma(adxValues, period);
        return (finalAdx, plusDiValues, minusDiValues);
    }

    public TrendSignalResult EvaluateTrendSignal(List<MarketData> data, int minConditions = 4)
    {
        var result = new TrendSignalResult { MaxScore = 6 };

        if (data.Count < 10)
        {
            result.Signal = "⏳ NEUTRAL";
            result.Direction = "NEUTRAL";
            return result;
        }

        var last = data.Last();
        var buyConds = new Dictionary<string, bool>
        {
            ["Price above 200 EMA"] = last.Close > last.Ema200,
            ["50 EMA above 200 EMA (Golden X)"] = last.Ema50 > last.Ema200,
            ["Price at/above 50 EMA"] = last.Close >= last.Ema50 * 0.9985,
            ["RSI 45–70 (bullish momentum)"] = last.Rsi >= 45 && last.Rsi <= 70,
            ["MACD above Signal line"] = last.Macd > last.MacdSig,
            ["Strong trend (ADX > 25)"] = last.Adx > 25
        };

        var sellConds = new Dictionary<string, bool>
        {
            ["Price below 200 EMA"] = last.Close < last.Ema200,
            ["50 EMA below 200 EMA (Death X)"] = last.Ema50 < last.Ema200,
            ["Price at/below 50 EMA"] = last.Close <= last.Ema50 * 1.0015,
            ["RSI 30–45 (bearish momentum)"] = last.Rsi >= 30 && last.Rsi <= 45,
            ["MACD below Signal line"] = last.Macd < last.MacdSig,
            ["Strong trend (ADX > 25)"] = last.Adx > 25
        };

        int bs = buyConds.Values.Count(v => v);
        int ss = sellConds.Values.Count(v => v);

        if (bs >= minConditions && bs >= ss)
        {
            if (bs >= 5)
            {
                result.Signal = "🚀 STRONG BUY";
                result.Direction = "STRONG_BUY";
            }
            else
            {
                result.Signal = "📈 BUY";
                result.Direction = "BUY";
            }
            result.Score = bs;
            result.Conditions = buyConds;
        }
        else if (ss >= minConditions && ss > bs)
        {
            if (ss >= 5)
            {
                result.Signal = "🔻 STRONG SELL";
                result.Direction = "STRONG_SELL";
            }
            else
            {
                result.Signal = "📉 SELL";
                result.Direction = "SELL";
            }
            result.Score = ss;
            result.Conditions = sellConds;
        }
        else
        {
            result.Signal = "⏳ NEUTRAL";
            result.Direction = "NEUTRAL";
            result.Score = Math.Max(bs, ss);
        }

        result.Close = last.Close;
        result.Ema50 = last.Ema50;
        result.Ema200 = last.Ema200;
        result.Rsi = last.Rsi;
        result.Macd = last.Macd;
        result.Adx = last.Adx;

        return result;
    }
}