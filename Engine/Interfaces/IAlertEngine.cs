using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IAlertEngine
{
    List<TradingAlert> Evaluate(TradingDecision? decision);
}