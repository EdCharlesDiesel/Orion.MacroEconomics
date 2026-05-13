using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Entities
{
    public class SimpleLatencyModel : ILatencyModel
    {
        public Task<decimal> SimulateLatencyMsAsync()
        {
            var random = new Random();

            // Simulate 10–150ms latency
            var latency = 10 + (decimal)(random.NextDouble() * 140);

            return Task.FromResult(latency);
        }
    }
}
