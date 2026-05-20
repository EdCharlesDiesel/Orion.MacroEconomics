using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Repository.Interfaces;

public interface IForexEventRepository
{
    Task<ForexEvent?> GetByIdAsync(Guid id);
    Task<ForexEvent?> GetByEventIdAsync(string eventId);
    Task<IEnumerable<ForexEvent>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<ForexEvent>> GetByCurrencyAsync(string currency, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<ForexEvent>> GetUpcomingEventsAsync(int days = 7);
    Task AddAsync(ForexEvent forexEvent);
    Task UpdateAsync(ForexEvent forexEvent);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string eventId);
    Task<int> GetCountAsync();
}

