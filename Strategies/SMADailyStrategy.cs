using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Strategies;
public class DailySmaPivotStrategy(ILogger<DailySmaPivotStrategy> logger)
{
    // Strategy parameters
    private const int SMA_200 = 200;
    private const int SMA_55 = 55;
    private const int SMA_21 = 21;
    private const int SMA_8 = 8;
    private const int SMA_5 = 5;

    public async Task<StrategySignal> AnalyzeSignalAsync(
        string symbol,
        List<OhlcvBar> dailyBars,
        PivotLevels pivots,
        decimal atr,
        CancellationToken ct = default)
    {
        try
        {
            var latestBar = dailyBars.Last();
            var smaValues = CalculateSMAs(dailyBars);

            var signal = new StrategySignal
            {
                Symbol = symbol,
                SmaValues = smaValues,
                PivotLevels = pivots,
                SignalTime = DateTime.UtcNow
            };

            // Check for long signal
            if (IsLongSignal(latestBar.Close, smaValues, pivots))
            {
                signal.Direction = "LONG";
                signal.Entry = latestBar.Close;
                signal.Confidence = CalculateConfidence(smaValues, pivots, "LONG");

                var tradeLevels = CalculateTradeLevels(signal.Entry, pivots, "LONG", atr);
                signal.StopLoss = tradeLevels.StopLoss;
                signal.TakeProfits = tradeLevels.TakeProfits;

                logger.LogInformation("Long signal generated for {Symbol} with confidence {Confidence}%",
                    symbol, signal.Confidence);
            }
            // Check for short signal
            else if (IsShortSignal(latestBar.Close, smaValues, pivots))
            {
                signal.Direction = "SHORT";
                signal.Entry = latestBar.Close;
                signal.Confidence = CalculateConfidence(smaValues, pivots, "SHORT");

                var tradeLevels = CalculateTradeLevels(signal.Entry, pivots, "SHORT", atr);
                signal.StopLoss = tradeLevels.StopLoss;
                signal.TakeProfits = tradeLevels.TakeProfits;

                logger.LogInformation("Short signal generated for {Symbol} with confidence {Confidence}%",
                    symbol, signal.Confidence);
            }

            return signal;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing signal for {Symbol}", symbol);
            return new StrategySignal { Status = "ERROR", RejectionReason = ex.Message };
        }
    }

    private Dictionary<int, decimal> CalculateSMAs(List<OhlcvBar> bars)
    {
        var smaValues = new Dictionary<int, decimal>();
        var closes = bars.Select(b => b.Close).ToList();

        smaValues[SMA_200] = CalculateSMA(closes, SMA_200);
        smaValues[SMA_55] = CalculateSMA(closes, SMA_55);
        smaValues[SMA_21] = CalculateSMA(closes, SMA_21);
        smaValues[SMA_8] = CalculateSMA(closes, SMA_8);
        smaValues[SMA_5] = CalculateSMA(closes, SMA_5);

        return smaValues;
    }

    private decimal CalculateSMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.Last();
        return values.TakeLast(period).Average();
    }

    private bool IsLongSignal(decimal currentPrice, Dictionary<int, decimal> sma, PivotLevels pivots)
    {
        // Trend alignment: 5 > 8 > 21 > 55 > 200
        bool trendAligned = sma[SMA_5] > sma[SMA_8] &&
                           sma[SMA_8] > sma[SMA_21] &&
                           sma[SMA_21] > sma[SMA_55] &&
                           sma[SMA_55] > sma[SMA_200];

        // Price above major SMAs
        bool aboveMajorSMAs = currentPrice > sma[SMA_200] && currentPrice > sma[SMA_55];

        // Pullback to support zone (near S1 or S2)
        bool atPivotSupport = currentPrice <= pivots.S1 * 1.002m &&
                              currentPrice >= pivots.S2 * 0.998m;

        return trendAligned && aboveMajorSMAs && atPivotSupport;
    }

    private bool IsShortSignal(decimal currentPrice, Dictionary<int, decimal> sma, PivotLevels pivots)
    {
        // Trend alignment: 5 < 8 < 21 < 55 < 200
        bool trendAligned = sma[SMA_5] < sma[SMA_8] &&
                           sma[SMA_8] < sma[SMA_21] &&
                           sma[SMA_21] < sma[SMA_55] &&
                           sma[SMA_55] < sma[SMA_200];

        // Price below major SMAs
        bool belowMajorSMAs = currentPrice < sma[SMA_200] && currentPrice < sma[SMA_55];

        // Rally to resistance zone (near R1 or R2)
        bool atPivotResistance = currentPrice >= pivots.R1 * 0.998m &&
                                 currentPrice <= pivots.R2 * 1.002m;

        return trendAligned && belowMajorSMAs && atPivotResistance;
    }

    private (decimal StopLoss, List<decimal> TakeProfits) CalculateTradeLevels(
        decimal entry, PivotLevels pivots, string direction, decimal atr)
    {
        decimal stopLoss;
        var takeProfits = new List<decimal>();

        if (direction == "LONG")
        {
            // Stop below S2 or 1.5x ATR
            decimal stopFromPivot = pivots.S2 - (pivots.PP - pivots.S2);
            decimal stopFromATR = entry - (atr * 1.5m);
            stopLoss = Math.Min(stopFromPivot, stopFromATR);

            // Take profits at R1, R2, R3
            takeProfits.Add(pivots.R1);
            takeProfits.Add(pivots.R2);
            takeProfits.Add(pivots.R3);
        }
        else // SHORT
        {
            // Stop above R2 or 1.5x ATR
            decimal stopFromPivot = pivots.R2 + (pivots.R2 - pivots.PP);
            decimal stopFromATR = entry + (atr * 1.5m);
            stopLoss = Math.Max(stopFromPivot, stopFromATR);

            // Take profits at S1, S2, S3
            takeProfits.Add(pivots.S1);
            takeProfits.Add(pivots.S2);
            takeProfits.Add(pivots.S3);
        }

        return (stopLoss, takeProfits);
    }

    private int CalculateConfidence(Dictionary<int, decimal> sma, PivotLevels pivots, string direction)
    {
        int confidence = 0;

        if (direction == "LONG")
        {
            // SMA alignment score (40 max)
            if (sma[SMA_5] > sma[SMA_8]) confidence += 10;
            if (sma[SMA_8] > sma[SMA_21]) confidence += 10;
            if (sma[SMA_21] > sma[SMA_55]) confidence += 10;
            if (sma[SMA_55] > sma[SMA_200]) confidence += 10;

            // Pivot strength (30 max)
            decimal s1s2Spread = Math.Abs(pivots.S1 - pivots.S2) / pivots.PP;
            if (s1s2Spread > 0.01m) confidence += 15;
            if (pivots.S1 > pivots.S2) confidence += 15;

            // Additional factors (30 max)
            if (sma[SMA_21] > sma[SMA_200] * 1.02m) confidence += 10;
            if (sma[SMA_8] > sma[SMA_21] * 1.005m) confidence += 10;
            if (pivots.S1 > pivots.PP) confidence += 10;
        }
        else // SHORT
        {
            // SMA alignment score (40 max)
            if (sma[SMA_5] < sma[SMA_8]) confidence += 10;
            if (sma[SMA_8] < sma[SMA_21]) confidence += 10;
            if (sma[SMA_21] < sma[SMA_55]) confidence += 10;
            if (sma[SMA_55] < sma[SMA_200]) confidence += 10;

            // Pivot strength (30 max)
            decimal r1r2Spread = Math.Abs(pivots.R1 - pivots.R2) / pivots.PP;
            if (r1r2Spread > 0.01m) confidence += 15;
            if (pivots.R1 < pivots.R2) confidence += 15;

            // Additional factors (30 max)
            if (sma[SMA_21] < sma[SMA_200] * 0.98m) confidence += 10;
            if (sma[SMA_8] < sma[SMA_21] * 0.995m) confidence += 10;
            if (pivots.R1 < pivots.PP) confidence += 10;
        }

        return confidence;
    }
}