using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Services
{
    public class SimpleNewsEventService : INewsEventService
    {
        public Task<bool> IsHighImpactEventAsync(DateTime time)
        {
            // Placeholder logic:
            // You should replace with TradingEconomics calendar

            var hour = time.Hour;

            // Simulate common macro release windows
            var isEvent = hour == 12 || hour == 14;

            return Task.FromResult(isEvent);
        }
    }
}
