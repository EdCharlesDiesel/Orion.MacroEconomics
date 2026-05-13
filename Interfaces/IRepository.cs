namespace Orion.MacroEconomics.Interfaces
{


    public interface IRepository<T>
    {
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAllAsync();
    }
}
