using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IAdvancedExecutionEngine
{
    Task<ExecutionResult> ExecuteAsync(string pair, string direction, decimal size, CancellationToken cancellationToken = default);
}