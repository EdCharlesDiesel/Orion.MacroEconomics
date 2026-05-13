using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IAlphaVantageSignalEngine
{
    Task<TradingSignalDocument> GenerateSignalAsync(
        string pair,
        CancellationToken cancellationToken = default);
}