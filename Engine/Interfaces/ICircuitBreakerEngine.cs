using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

/// <summary>
/// Contract for circuit breaker evaluation.
/// </summary>
public interface ICircuitBreakerEngine
{
    CircuitBreakerResult Evaluate(
        AccountContext account,
        List<TradePlan>? todayTrades,
        List<TradePlan>? openTrades,
        DataQualityResult? dataQuality);
}