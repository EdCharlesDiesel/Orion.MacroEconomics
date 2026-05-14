using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using RegimeResult = Orion.MacroEconomics.Entities.RegimeResult;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Detects and simulates market regimes.
    /// </summary>
    public interface IRegimeEngine
    {
        /// <summary>
        /// Returns the next likely regime from the current regime.
        /// </summary>
        MarketRegime Next(MarketRegime current);

        /// <summary>
        /// Detects the current regime from a normalized indicator.
        /// </summary>
        RegimeResult Detect(NormalizedIndicator normalized);

        object? Analyze(RegimeInput input);
    }
}