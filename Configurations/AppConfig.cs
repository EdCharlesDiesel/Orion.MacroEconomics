namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Static, pair-level FX trading constants (pip size, per-pair ATR multipliers, per-pair minimum stop distance).
/// Used by <c>SignalGenerator</c> and other strategy components that need pair-specific calibration.
/// </summary>
public static class AppConfig
{
    public const decimal DefaultPipSize    = 0.0001m;
    public const decimal JpyPipSize        = 0.01m;
    public const decimal DefaultMinStop    = 0.0010m;
    public const decimal DefaultAtrMult    = 2.0m;

    /// <summary>Per-pair ATR-based stop-loss multipliers. Lookup falls back to the strategy default.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> PairAtrMultipliers =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["EURUSD"] = 1.8m,
            ["GBPUSD"] = 2.0m,
            ["USDJPY"] = 2.2m,
            ["USDCHF"] = 1.8m,
            ["AUDUSD"] = 2.0m,
            ["NZDUSD"] = 2.0m,
            ["USDCAD"] = 2.0m,
            ["EURJPY"] = 2.4m,
            ["GBPJPY"] = 2.6m,
            ["XAUUSD"] = 1.5m,
        };

    /// <summary>Per-pair minimum stop distance (in price units). Lookup falls back to <see cref="DefaultMinStop"/>.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> PairMinStop =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["EURUSD"] = 0.0010m,
            ["GBPUSD"] = 0.0012m,
            ["USDJPY"] = 0.10m,
            ["USDCHF"] = 0.0010m,
            ["AUDUSD"] = 0.0010m,
            ["NZDUSD"] = 0.0010m,
            ["USDCAD"] = 0.0010m,
            ["EURJPY"] = 0.12m,
            ["GBPJPY"] = 0.15m,
            ["XAUUSD"] = 1.00m,
        };

    /// <summary>Returns the pip size for the given pair. JPY-quote and metals use larger units; everything else defaults to 0.0001.</summary>
    public static decimal PipSize(string? pair)
    {
        if (string.IsNullOrWhiteSpace(pair))
            return DefaultPipSize;

        var p = pair.Trim().ToUpperInvariant();

        if (p.EndsWith("JPY", StringComparison.Ordinal)) return JpyPipSize;
        if (p == "XAUUSD" || p == "GOLD")                return 0.10m;
        if (p == "XAGUSD" || p == "SILVER")              return 0.01m;

        return DefaultPipSize;
    }
}
