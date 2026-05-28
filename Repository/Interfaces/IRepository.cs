using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Repository.Interfaces
{


    public interface IRepository<T>
    {
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAllAsync();
        Task<List<TradeExecution>> GetAsync(Func<object, bool> func);
        Task AddAsync(StrategySignal signal);
    }
}
