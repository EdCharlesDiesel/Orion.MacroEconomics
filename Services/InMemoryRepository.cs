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
        throw new NotImplementedException();
    }

    Task<IEnumerable<T>> IRepository<T>.GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<T>> GetAllAsync()
        => Task.FromResult(_data.ToList());

    public Task SaveChangesAsync()
        => Task.CompletedTask;
}