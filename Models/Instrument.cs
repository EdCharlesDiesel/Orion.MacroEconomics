namespace Orion.MacroEconomics.Models;


public class Instrument
{
    public string Name { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public double PipValue { get; set; }
    public double PipSize { get; set; }
    public string Correlation { get; set; } = string.Empty;

    public static Dictionary<string, Instrument> GetInstruments() => new()
    {
        ["EUR/USD"] = new Instrument { Name = "EUR/USD", Ticker = "EURUSD=X", PipValue = 10.0, PipSize = 0.0001, Correlation = "DXY ↑ = bearish" },
        ["GBP/USD"] = new Instrument { Name = "GBP/USD", Ticker = "GBPUSD=X", PipValue = 10.0, PipSize = 0.0001, Correlation = "DXY ↑ = bearish" },
        ["AUD/USD"] = new Instrument { Name = "AUD/USD", Ticker = "AUDUSD=X", PipValue = 10.0, PipSize = 0.0001, Correlation = "Gold ↑ = bullish" },
        ["NZD/USD"] = new Instrument { Name = "NZD/USD", Ticker = "NZDUSD=X", PipValue = 10.0, PipSize = 0.0001, Correlation = "AUD/USD alignment" },
        ["USD/JPY"] = new Instrument { Name = "USD/JPY", Ticker = "USDJPY=X", PipValue = 9.09, PipSize = 0.01, Correlation = "US10Y yields aligned" },
        ["USD/CHF"] = new Instrument { Name = "USD/CHF", Ticker = "USDCHF=X", PipValue = 10.8, PipSize = 0.0001, Correlation = "EUR/USD inverse" },
        ["USD/CAD"] = new Instrument { Name = "USD/CAD", Ticker = "USDCAD=X", PipValue = 7.4, PipSize = 0.0001, Correlation = "Oil inverse" },
        ["EUR/GBP"] = new Instrument { Name = "EUR/GBP", Ticker = "EURGBP=X", PipValue = 12.5, PipSize = 0.0001, Correlation = "EUR/USD vs GBP/USD" },
        ["EUR/JPY"] = new Instrument { Name = "EUR/JPY", Ticker = "EURJPY=X", PipValue = 9.09, PipSize = 0.01, Correlation = "Risk-on sentiment" },
        ["GBP/JPY"] = new Instrument { Name = "GBP/JPY", Ticker = "GBPJPY=X", PipValue = 9.09, PipSize = 0.01, Correlation = "Volatility proxy" },
        ["AUD/JPY"] = new Instrument { Name = "AUD/JPY", Ticker = "AUDJPY=X", PipValue = 9.09, PipSize = 0.01, Correlation = "Risk appetite gauge" },
        ["🥇 Gold"] = new Instrument { Name = "🥇 Gold", Ticker = "GC=F", PipValue = 10.0, PipSize = 0.10, Correlation = "DXY inverse, VIX" },
        ["🥈 Silver"] = new Instrument { Name = "🥈 Silver", Ticker = "SI=F", PipValue = 10.0, PipSize = 0.01, Correlation = "Gold correlation" },
        ["🪙 Platinum"] = new Instrument { Name = "🪙 Platinum", Ticker = "PL=F", PipValue = 10.0, PipSize = 0.10, Correlation = "Industrial demand" }
    };

    public static readonly HashSet<string> TrendCommodities = new() { "🥇 Gold", "🥈 Silver", "🪙 Platinum" };

    public static readonly Dictionary<string, HashSet<string>> CorrelationGroups = new()
    {
        ["USD vs majors (same dir = stacked USD risk)"] = new() { "EUR/USD", "GBP/USD", "AUD/USD", "NZD/USD" },
        ["USD-base pairs (same dir = stacked USD risk)"] = new() { "USD/JPY", "USD/CHF", "USD/CAD" },
        ["JPY crosses (same dir = stacked JPY risk)"] = new() { "USD/JPY", "EUR/JPY", "GBP/JPY", "AUD/JPY" },
        ["Gold / Silver (highly correlated)"] = new() { "🥇 Gold", "🥈 Silver" }
    };
}

public class TrendTimeframe
{
    public string Label { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Resample { get; set; }

    public static Dictionary<string, TrendTimeframe> GetTimeframes() => new()
    {
        ["1 Hour"] = new TrendTimeframe { Label = "1 Hour", Interval = "60m", Period = "59d", Resample = "1h" },
        ["4 Hours"] = new TrendTimeframe { Label = "4 Hours", Interval = "60m", Period = "59d", Resample = "4h" },
        ["Daily"] = new TrendTimeframe { Label = "Daily", Interval = "1d", Period = "2y", Resample = null },
        ["Weekly"] = new TrendTimeframe { Label = "Weekly", Interval = "1d", Period = "2y", Resample = "1W" }
    };
}