using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    public interface ICorrelationEngine
    {
        Task<CorrelationResult> AnalyzeAsync(CorrelationRequest request,CancellationToken cancellationToken = default);
    }
}
