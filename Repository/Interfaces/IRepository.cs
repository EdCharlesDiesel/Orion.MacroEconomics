namespace Orion.MacroEconomics.Repository.Interfaces
{


    public interface IRepository<T>
    {
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAllAsync();
    }
}
