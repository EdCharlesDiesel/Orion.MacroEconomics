using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Providers;

public class OrderBookProvider: IOrderBookProvider
{
    public Task<OrderBook> GetOrderBookAsync(string pair)
    {
        throw new NotImplementedException();
    }
}