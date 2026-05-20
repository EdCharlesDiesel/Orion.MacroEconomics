using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Repository.Interfaces;
namespace Orion.MacroEconomics.Repository;

public class ForexNewsRepository(IDocumentSession session, ILogger<ForexNewsRepository> logger) : IForexNewsRepository
{
    private readonly ILogger<ForexNewsRepository> _logger = logger;

    public async Task<ForexNews?> GetByIdAsync(Guid id)
    {
        return await session.LoadAsync<ForexNews>(id);
    }

    public async Task<ForexNews?> GetByNewsIdAsync(string newsId)
    {
        return await session.Query<ForexNews>()
            .FirstOrDefaultAsync(n => n.NewsId == newsId);
    }

    public async Task<IEnumerable<ForexNews>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await session.Query<ForexNews>()
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<ForexNews>> GetByCurrencyAsync(string currency, DateTime? from = null)
    {
        var query = session.Query<ForexNews>()
            .Where(n => n.AffectedCurrencies.Contains(currency));

        if (from.HasValue)
            query = query.Where(n => n.PublishedAt >= from.Value);

        return await query.OrderByDescending(n => n.PublishedAt).ToListAsync();
    }

    public async Task<IEnumerable<ForexNews>> GetLatestNewsAsync(int count = 20)
    {
        return await session.Query<ForexNews>()
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddAsync(ForexNews news)
    {
        news.RetrievedAt = DateTime.UtcNow;
        session.Store(news);
        await session.SaveChangesAsync();
    }

    public async Task UpdateAsync(ForexNews news)
    {
        session.Update(news);
        await session.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        session.Delete<ForexNews>(id);
        await session.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string newsId)
    {
        return await session.Query<ForexNews>()
            .AnyAsync(n => n.NewsId == newsId);
    }
}