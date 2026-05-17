using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Analyzes closed trades and produces performance metrics.
    /// </summary>
    public interface IPerformanceAnalyticsEngine
    {
        /// <summary>
        /// Calculates win rate, profit factor, expectancy, drawdown and risk reward.
        /// </summary>
        PerformanceReport Analyze(List<TradePlan>? trades);
    }
}