using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Services;

public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _data = [];

    public Task AddAsync(T entity)
    {
        _data.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<T> entities)
    {
        _data.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<T>> GetAllAsync()
        => Task.FromResult<IEnumerable<T>>(_data.ToList());

    public Task<IEnumerable<T>> GetAsync(Func<T, bool> predicate)
        => Task.FromResult<IEnumerable<T>>(_data.Where(predicate).ToList());
}
