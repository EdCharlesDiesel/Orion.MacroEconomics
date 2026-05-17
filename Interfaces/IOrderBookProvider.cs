using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
{
    public interface IOrderBookProvider
    {
        Task<OrderBook> GetOrderBookAsync(string pair);
    }

    public interface ILatencyModel
    {
        Task<decimal> SimulateLatencyMsAsync();
    }

    public interface INewsEventService
    {
        Task<bool> IsHighImpactEventAsync(DateTime time);
    }
}
