using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers;

public static class CreateValid
{
    private const decimal DefaultStartPrice = 1.1000m;
    private const decimal DefaultStep       = 0.0005m;
    private const decimal DefaultVolume     = 1000m;
    private const string  DefaultPair       = "EURUSD";
    private const string  DefaultTimeframe  = "1d";
    private const string  DefaultSource     = "Synthetic";

    public static List<OhlcvBar> CreateValidCandles(
        int count           = 60,
        decimal startPrice  = DefaultStartPrice,
        decimal step        = DefaultStep,
        decimal volume      = DefaultVolume,
        string pair         = DefaultPair,
        string timeframe    = DefaultTimeframe,
        string source       = DefaultSource,
        DateTime? startUtc  = null)
    {
        if (count <= 0)
            return new List<OhlcvBar>(0);

        var candles = new List<OhlcvBar>(count);
        var start   = startUtc ?? DateTime.UtcNow.AddDays(-count);

        decimal price = startPrice;

        for (int i = 0; i < count; i++)
        {
            var open  = price;
            var close = price + step;
            var high  = Math.Max(open, close) + step;
            var low   = Math.Min(open, close) - step;

            candles.Add(new OhlcvBar
            {
                Pair         = pair,
                Timeframe    = timeframe,
                Source       = source,
                TimestampUtc = start.AddDays(i),
                Open         = open,
                High         = high,
                Low          = low,
                Close        = close,
                Volume       = volume
            });

            price = close;
        }

        return candles;
    }
}