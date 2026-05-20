using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Repository;

public class ForexEventRepository(IDocumentSession session, ILogger<ForexEventRepository> logger) : IForexEventRepository
{
    private readonly ILogger<ForexEventRepository> _logger = logger;

    public async Task<ForexEvent?> GetByIdAsync(Guid id)
    {
        return await session.LoadAsync<ForexEvent>(id);
    }

    public async Task<ForexEvent?> GetByEventIdAsync(string eventId)
    {
        return await session.Query<ForexEvent>()
            .FirstOrDefaultAsync(e => e.EventId == eventId);
    }

    public async Task<IEnumerable<ForexEvent>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await session.Query<ForexEvent>()
            .OrderByDescending(e => e.EventDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ForexEvent>> GetByCurrencyAsync(string currency, DateTime? from = null, DateTime? to = null)
    {
        var query = session.Query<ForexEvent>()
            .Where(e => e.Currency == currency);

        if (from.HasValue)
            query = query.Where(e => e.EventDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.EventDate <= to.Value);

        return await query.OrderByDescending(e => e.EventDate).ToListAsync();
    }

    public async Task<IEnumerable<ForexEvent>> GetUpcomingEventsAsync(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await session.Query<ForexEvent>()
            .Where(e => e.EventDate >= DateTime.UtcNow && e.EventDate <= cutoff)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }

    public async Task AddAsync(ForexEvent forexEvent)
    {
        forexEvent.RetrievedAt = DateTime.UtcNow;
        session.Store(forexEvent);
        await session.SaveChangesAsync();
    }

    public async Task UpdateAsync(ForexEvent forexEvent)
    {
        session.Update(forexEvent);
        await session.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        session.Delete<ForexEvent>(id);
        await session.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await session.Query<ForexEvent>()
            .AnyAsync(e => e.EventId == eventId);
    }

    public async Task<int> GetCountAsync()
    {
        return await session.Query<ForexEvent>().CountAsync();
    }
}