using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers;

public class MarketDataRejectedException : Exception
{
    public string Pair { get; }
    public string Reason { get; }
    public DataQualityResult QualityResult { get; }

    public MarketDataRejectedException(
        string pair,
        string reason,
        DataQualityResult qualityResult)
        : base(BuildMessage(pair, reason, qualityResult))
    {
        Pair          = pair;
        Reason        = reason;
        QualityResult = qualityResult;
    }

    public bool CanRetry => QualityResult.CanRetry;

    public int QualityScore => QualityResult.Score;

    private static string BuildMessage(string pair, string reason, DataQualityResult result)
    {
        var msg = $"Market data rejected for {pair}: {reason}";
        if (result?.Score > 0)
            msg += $" (score={result.Score}, canRetry={result.CanRetry})";
        return msg;
    }
}