using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces;


public interface ILiveTradingOrchestrator
{
    LiveTradingResult Run(ForexMarketInput input, AccountContext account, OrderBook orderBook);
}