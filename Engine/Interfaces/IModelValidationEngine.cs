
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Validates whether a trading model is suitable for production or paper trading.
    /// </summary>
    public interface IModelValidationEngine
    {
        /// <summary>
        /// Validates model performance using performance metrics and closed trades.
        /// </summary>
        ModelValidationReport Validate(
            PerformanceReport performance,
            List<TradePlan> trades);
    }
}