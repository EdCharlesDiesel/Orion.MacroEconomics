using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Repository.Interfaces;

public interface IForexNewsRepository
{
    Task<ForexNews?> GetByIdAsync(Guid id);
    Task<ForexNews?> GetByNewsIdAsync(string newsId);
    Task<IEnumerable<ForexNews>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<ForexNews>> GetByCurrencyAsync(string currency, DateTime? from = null);
    Task<IEnumerable<ForexNews>> GetLatestNewsAsync(int count = 20);
    Task AddAsync(ForexNews news);
    Task UpdateAsync(ForexNews news);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string newsId);
}