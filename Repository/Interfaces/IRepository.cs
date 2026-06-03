namespace Orion.MacroEconomics.Repository.Interfaces;

public interface IRepository<T>
{
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAsync(Func<T, bool> predicate);
}
