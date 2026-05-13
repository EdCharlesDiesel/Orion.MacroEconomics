using Orion.MacroEconomics.Engine;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Services
{
    public class PortfolioExecutor
    {
        private readonly ExecutionEngine _engine;

        public PortfolioExecutor(ExecutionEngine engine)
        {
            _engine = engine;
        }

        public async Task<List<ExecutionOrder>> ExecutePortfolio(List<PortfolioPosition> positions)
        {
            var orders = new List<ExecutionOrder>();

            foreach (var p in positions)
            {
                var order = await _engine.ExecuteAsync(
                    p.Pair,
                    p.Direction,
                    p.PositionSize);

                orders.Add(order);
            }

            return orders;
        }
    }
}
