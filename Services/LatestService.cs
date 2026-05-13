using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Services
{
    public sealed class LatestService : ILatestService
    {
        public async Task<string> GetLatestUpdatesAsync()
        {
            return await HttpRequesterClass.HttpRequester("/updates");
        }

        public async Task<string> GetLatestUpdatesByDateAsync(DateTime startDate)
        {
            var date = startDate.ToString("yyyy-MM-dd");
            return await HttpRequesterClass.HttpRequester($"/updates/{date}");
        }
    }
}