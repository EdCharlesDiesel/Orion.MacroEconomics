using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    public interface ILiquidityEngine
    {
        Task<LiquidityResult> AnalyzeAsync(LiquidityRequest request,CancellationToken cancellationToken = default);
    }
}
