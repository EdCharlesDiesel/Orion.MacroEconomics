using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    /// <summary>
    /// Evaluates whether a trade signal is allowed based on market and regime risk.
    /// </summary>
    public interface IRiskEngine
    {
        /// <summary>
        /// Calculates spread, volatility, regime, and drawdown risk.
        /// </summary>
        RiskResult Evaluate(SignalResult signal, NormalizedMarketContext? market,
            RegimeResult regime);
    }
}