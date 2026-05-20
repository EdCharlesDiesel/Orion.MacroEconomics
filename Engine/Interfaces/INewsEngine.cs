using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface INewsEngine
{
    Task<IEnumerable<ForexEvent>> FetchAndProcessEventsAsync();
    Task<IEnumerable<ForexNews>> FetchAndProcessNewsAsync();
    Task<Dictionary<string, List<ForexEvent>>> GetEventsByCurrencyAsync(DateTime? from = null);
    Task<List<ForexEvent>> GetHighImpactEventsAsync(int days = 3);
    Task<Dictionary<string, decimal>> CalculateCurrencyStrengthAsync();
    Task<ForexEvent?> GetNextMajorEventAsync(string? currency = null);
}