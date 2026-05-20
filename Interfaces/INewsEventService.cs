namespace Orion.MacroEconomics.Interfaces;

public interface INewsEventService
{
    Task<bool> IsHighImpactEventAsync(DateTime time);
}