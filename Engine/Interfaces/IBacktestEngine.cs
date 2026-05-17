using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

/// <summary>
/// Contract for running historical backtests.
/// </summary>
public interface IBacktestEngine
{
    Task<List<TradeResult>> RunAsync(DateTime start, DateTime end, decimal capital);
}