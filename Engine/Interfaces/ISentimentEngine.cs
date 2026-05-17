using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Analyzes sentiment items and produces a directional sentiment result.
    /// </summary>
    public interface ISentimentEngine
    {
        /// <summary>
        /// Calculates weighted sentiment score, bias, confidence and reasons.
        /// </summary>
        Task<SentimentResult> AnalyzeAsync(SentimentRequest request, CancellationToken cancellationToken = default);
    }
}
