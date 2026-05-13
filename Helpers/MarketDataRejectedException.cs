using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers;

public class MarketDataRejectedException : Exception
{
    public string Pair { get; }
    public DataQualityResult QualityResult { get; }

    public MarketDataRejectedException(
        string pair, 
        string reason,
        DataQualityResult qualityResult) 
        : base($"Market data rejected for {pair}: {reason}")
    {
        Pair = pair;
        QualityResult = qualityResult;
    }
}