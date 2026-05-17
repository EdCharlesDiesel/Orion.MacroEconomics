using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine
{
    /// <summary>
    /// Runs rolling walk-forward backtests using fixed train and test periods.
    /// </summary>
    public sealed class WalkForwardEngine(BacktestEngine engine) : IWalkForwardEngine
    {
        private readonly BacktestEngine _engine = engine ?? throw new ArgumentNullException(nameof(engine));


        public async Task<List<WalkForwardResult>> RunAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            if (end <= start)
                throw new ArgumentException("End date must be greater than start date.", nameof(end));

            var window = TimeSpan.FromDays(90);
            var trainLength = TimeSpan.FromDays(60);
            var testLength = TimeSpan.FromDays(30);
            var results = new List<WalkForwardResult>();

            for (var current = start; current <= end - window; current = current.Add(window))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trainStart = current;
                var trainEnd = current.Add(trainLength);
                var testStart = trainEnd;
                var testEnd = trainEnd.Add(testLength);

                var trainTrades = await _engine.RunAsync(trainStart, trainEnd, 100000m);
                var testTrades = await _engine.RunAsync(testStart, testEnd, 100000m);

                results.Add(new WalkForwardResult
                {
                    TrainStart = trainStart,
                    TrainEnd = trainEnd,
                    TestStart = testStart,
                    TestEnd = testEnd,
                    TrainTradeCount = trainTrades.Count,
                    TestTradeCount = testTrades.Count,
                    TestTrades = testTrades
                });
            }

            return results;
        }
    }
}