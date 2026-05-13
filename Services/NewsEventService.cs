using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Services;

public class NewsEventService: INewsEventService
{
    public Task<bool> IsHighImpactEventAsync(DateTime time)
    {
        throw new NotImplementedException();
    }
}