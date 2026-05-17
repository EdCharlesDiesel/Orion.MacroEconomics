using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Runs walk-forward backtesting using rolling train and test windows.
    /// </summary>
    public interface IWalkForwardEngine
    {
        /// <summary>
        /// Executes walk-forward analysis over the supplied date range.
        /// </summary>
        Task<List<WalkForwardResult>> RunAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }
}