namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Static, compile-time market constants — asset ticker maps, timeframe
/// definitions, and pair-specific ATR / pip-size lookup tables.
///
/// These are intentionally <c>static readonly</c> because they represent
/// fixed market conventions, not user-configurable values. Runtime overrides
/// belong in <see cref="AppConfiguration"/>.
/// </summary>
public static class MarketConstants
{

    /// <summary>
    /// Display name → Polygon.io ticker (used for price feeds).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PolygonTickers =
        new Dictionary<string, string>
        {
            ["EUR/USD"] = "C:EURUSD",
            ["GBP/USD"] = "C:GBPUSD",
            ["USD/JPY"] = "C:USDJPY",
            ["USD/ZAR"] = "C:USDZAR",
            ["AUD/USD"] = "C:AUDUSD",
            ["NZD/USD"] = "C:NZDUSD",
            ["USD/CAD"] = "C:USDCAD",
            ["USD/CHF"] = "C:USDCHF",
            ["XAU/USD"] = "C:XAUUSD",
            ["BTC/USD"] = "X:BTCUSD",
        };

    /// <summary>
    /// Display name → (Polygon multiplier, Polygon timespan, history look-back in days).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, TimeframeDefinition> Timeframes =
        new Dictionary<string, TimeframeDefinition>
        {
            ["Weekly"]     = new(Multiplier: 1,  Timespan: "week",   HistoryDays: 730),
            ["Daily"]      = new(Multiplier: 1,  Timespan: "day",    HistoryDays: 92),
            ["4 Hour"]     = new(Multiplier: 4,  Timespan: "hour",   HistoryDays: 31),
            ["Hourly"]     = new(Multiplier: 1,  Timespan: "hour",   HistoryDays: 31),
            ["15 Minute"]  = new(Multiplier: 15, Timespan: "minute", HistoryDays: 5),
        };

    /// <summary>
    /// Per-pair ATR multipliers applied on top of the global
    /// <see cref="RiskSettings.AtrStopMultiplier"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, decimal> PairAtrMultipliers =
        new Dictionary<string, decimal>
        {
            ["EUR/USD"] = 1.5m,
            ["GBP/USD"] = 1.8m,
            ["USD/JPY"] = 1.5m,
            ["USD/ZAR"] = 2.5m,
            ["AUD/USD"] = 1.5m,
            ["NZD/USD"] = 1.6m,
            ["USD/CAD"] = 1.5m,
            ["USD/CHF"] = 1.5m,
            ["XAU/USD"] = 2.0m,
            ["BTC/USD"] = 2.0m,
        };

    /// <summary>
    /// Absolute minimum stop distance per pair (in price units).
    /// Falls back to <see cref="RiskSettings.DefaultMinStop"/> when a pair is absent.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, decimal> PairMinStop =
        new Dictionary<string, decimal>
        {
            ["EUR/USD"] = 0.0010m,
            ["GBP/USD"] = 0.0015m,
            ["USD/JPY"] = 0.10m,
            ["USD/ZAR"] = 0.05m,
            ["AUD/USD"] = 0.0010m,
            ["NZD/USD"] = 0.0010m,
            ["USD/CAD"] = 0.0010m,
            ["USD/CHF"] = 0.0010m,
            ["XAU/USD"] = 2.00m,
            ["BTC/USD"] = 500.0m,
        };

    /// <summary>
    /// Returns the pip (tick) size for the given display-name pair.
    /// </summary>
    public static decimal PipSize(string pair) => pair switch
    {
        _ when pair.Contains("JPY") => 0.01m,
        "XAU/USD"                   => 0.10m,
        "BTC/USD"                   => 1.0m,
        _ when pair.Contains("ZAR") => 0.001m,
        _                           => 0.0001m,
    };
}

/// <summary>
/// Polygon.io timeframe query parameters.
/// </summary>
public sealed record TimeframeDefinition(int Multiplier, string Timespan, int HistoryDays);