using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers
{
    public static class EquityBuilder
    {
        public static List<EquityPoint> Build(List<TradeResult> trades, decimal start)
        {
            if (trades == null)
                throw new ArgumentNullException(nameof(trades));

            var curve  = new List<EquityPoint>(trades.Count + 1);
            var equity = start;

            if (trades.Count == 0)
            {
                curve.Add(new EquityPoint { Time = DateTime.UtcNow, Equity = equity });
                return curve;
            }

            var ordered = trades.OrderBy(x => x.ExitTime).ToList();

            curve.Add(new EquityPoint
            {
                Time   = ordered[0].EntryTime,
                Equity = equity
            });

            foreach (var t in ordered)
            {
                equity += t.PnL;

                curve.Add(new EquityPoint
                {
                    Time   = t.ExitTime,
                    Equity = equity
                });
            }

            return curve;
        }

        public static decimal TotalPnL(IEnumerable<TradeResult> trades) =>
            trades.Sum(t => t.PnL);

        public static decimal AverageReturnPct(IEnumerable<TradeResult> trades)
        {
            var list = trades.Where(t => t.PositionSize != 0).ToList();
            return list.Count == 0 ? 0 : list.Average(t => t.ReturnPct);
        }

        public static (decimal Wins, decimal Losses) WinLossPnL(IEnumerable<TradeResult> trades)
        {
            decimal wins = 0, losses = 0;
            foreach (var t in trades)
            {
                if (t.PnL >= 0) wins   += t.PnL;
                else            losses += t.PnL;
            }
            return (wins, losses);
        }

        public static decimal AverageHoldingHours(IEnumerable<TradeResult> trades)
        {
            var list = trades.ToList();
            return list.Count == 0
                ? 0
                : (decimal)list.Average(t => (t.ExitTime - t.EntryTime).TotalHours);
        }

        public static decimal AveragePriceMove(IEnumerable<TradeResult> trades)
        {
            var list = trades.ToList();
            return list.Count == 0
                ? 0
                : list.Average(t => t.ExitPrice - t.EntryPrice);
        }

        public static IEnumerable<string> Pairs(IEnumerable<TradeResult> trades) =>
            trades.Where(t => !string.IsNullOrWhiteSpace(t.Pair))
                  .Select(t => t.Pair)
                  .Distinct();
    }
}