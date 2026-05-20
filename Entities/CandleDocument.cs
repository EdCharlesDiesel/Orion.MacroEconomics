namespace Orion.MacroEconomics.Entities;

/// <summary>
/// Marten document that holds all OHLCV candles for a single pair/timeframe
/// combination. Stored and replaced as one unit on every sync.
/// Id format: "{pair}:{timeframe}" e.g. "EUR/USD:Daily"
/// </summary>
public sealed class CandleDocument
{
    public string Id { get; set; } = string.Empty;
    public string Pair { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public List<Candle> Candles { get; set; } = [];
    public DateTime LastUpdated { get; set; }
    public static string BuildId(string pair, string timeframe) => $"{pair}:{timeframe}";
}