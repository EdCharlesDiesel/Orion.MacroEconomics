using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces;

public interface IOrderBookExecutionService
{
     ExecutionOrder Execute(OrderBook book, string direction, decimal size);
}