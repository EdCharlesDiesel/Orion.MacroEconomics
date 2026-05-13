using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Engine;

public sealed class AlphaVantageSignalEngine(    IAlphaVantageMarketDataProvider provider,    IMarketDataDocumentStore store,    ILogger<AlphaVantageSignalEngine> logger) : IAlphaVantageSignalEngine
{
    public async Task<TradingSignalDocument> GenerateSignalAsync(
        string pair,
        CancellationToken cancellationToken = default)
    {
        var candles = await provider.GetDailyFxCandlesAsync(pair, cancellationToken);

        if (candles.Count < 50)
            throw new InvalidOperationException("At least 50 candles are required.");

        var orderedCandles = candles
            .OrderBy(x => x.TimestampUtc)
            .ToList();

        await store.StoreMarketDataAsync(
            pair,
            orderedCandles,
            orderedCandles.First().TimestampUtc,
            orderedCandles.Last().TimestampUtc,
            cancellationToken);

        var closes = orderedCandles.Select(x => x.Close).ToList();

        var fastSma = closes.TakeLast(20).Average();
        var slowSma = closes.TakeLast(50).Average();
        var lastClose = closes.Last();

        var previousCandle = orderedCandles[^2];

        var pivotPoint = (previousCandle.High + previousCandle.Low + previousCandle.Close) / 3m;
        var resistance1 = (2m * pivotPoint) - previousCandle.Low;
        var support1 = (2m * pivotPoint) - previousCandle.High;
        var resistance2 = pivotPoint + (previousCandle.High - previousCandle.Low);
        var support2 = pivotPoint - (previousCandle.High - previousCandle.Low);

        var relativeStrength = slowSma == 0
            ? 0
            : Math.Abs((fastSma - slowSma) / slowSma) * 100m;

        var direction = ResolveDirection(
            fastSma,
            slowSma,
            lastClose,
            pivotPoint);

        var signal = new TradingSignalDocument
        {
            Pair = pair.Trim().ToUpperInvariant(),
            Direction = direction,
            Confidence = Math.Min(100m, Math.Round(relativeStrength * 10m, 2)),
            FastSma = Math.Round(fastSma, 5),
            SlowSma = Math.Round(slowSma, 5),
            LastClose = Math.Round(lastClose, 5),

            PivotPoint = Math.Round(pivotPoint, 5),
            Support1 = Math.Round(support1, 5),
            Support2 = Math.Round(support2, 5),
            Resistance1 = Math.Round(resistance1, 5),
            Resistance2 = Math.Round(resistance2, 5),

            Reason = BuildReason(direction, fastSma, slowSma, lastClose, pivotPoint),
            CreatedUtc = DateTime.UtcNow
        };

        await store.StoreSignalAsync(signal, cancellationToken);

        logger.LogInformation(
            "Generated {Direction} signal for {Pair}",
            signal.Direction,
            signal.Pair);

        return signal;
    }

    private static string ResolveDirection(
        decimal fastSma,
        decimal slowSma,
        decimal lastClose,
        decimal pivotPoint)
    {
        if (fastSma > slowSma && lastClose > pivotPoint)
            return "BUY";

        if (fastSma < slowSma && lastClose < pivotPoint)
            return "SELL";

        return "NEUTRAL";
    }

    private static string BuildReason(
        string direction,
        decimal fastSma,
        decimal slowSma,
        decimal lastClose,
        decimal pivotPoint)
    {
        return direction switch
        {
            "BUY" =>
                "20-day SMA is above 50-day SMA and price is above the pivot point.",

            "SELL" =>
                "20-day SMA is below 50-day SMA and price is below the pivot point.",

            _ =>
                $"Mixed signal. Fast SMA: {Math.Round(fastSma, 5)}, Slow SMA: {Math.Round(slowSma, 5)}, Last Close: {Math.Round(lastClose, 5)}, Pivot: {Math.Round(pivotPoint, 5)}."
        };
    }
}