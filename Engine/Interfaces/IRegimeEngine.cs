using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Engine.Interfaces;

/// <summary>
/// Contract for the macro regime detection and simulation engine.
/// </summary>
public interface IRegimeEngine
{
    /// <summary>
    /// Simulates the next market regime from <paramref name="current"/> using
    /// a calibrated first-order Markov transition matrix.
    /// </summary>
    /// <param name="current">The regime that is active right now.</param>
    /// <returns>The regime for the next period (may be the same as <paramref name="current"/>).</returns>
    MarketRegime Next(MarketRegime current);

    /// <summary>
    /// Detects the implied market regime from a single normalised macro indicator.
    /// </summary>
    /// <param name="normalized">The normalised indicator reading.</param>
    /// <returns>
    /// A <see cref="RegimeResult"/> containing the detected regime, a confidence
    /// score, and a plain-English explanation of the dominant signal.
    /// </returns>
    RegimeResult Detect(NormalizedIndicator normalized);

    /// <summary>
    /// Performs a full multi-indicator regime analysis.
    /// When <see cref="RegimeInput.Indicators"/> is empty the engine falls back to
    /// a Markov simulation seeded from <see cref="RegimeInput.CurrentRegime"/>.
    /// </summary>
    /// <param name="input">The analysis request, including the current regime and indicator set.</param>
    /// <returns>
    /// An aggregated <see cref="RegimeResult"/> whose confidence is the normalised
    /// vote share of the winning regime across all supplied indicators.
    /// </returns>
    RegimeResult Analyze(RegimeInput input);
}