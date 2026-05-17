namespace Orion.MacroEconomics.Configurations;

public class AppConfig
{
    public const string Version = "1.0.0";

    /// <summary>Pair display name → Massive.com forex ticker (C:EURUSD etc.)</summary>
    public static readonly Dictionary<string, string> Assets = new()
    {
        { "EUR/USD", "C:EURUSD" },
        { "GBP/USD", "C:GBPUSD" },
        { "USD/JPY", "C:USDJPY" },
        { "USD/ZAR", "C:USDZAR" },
        { "AUD/USD", "C:AUDUSD" },
        { "NZD/USD", "C:NZDUSD" },
        { "USD/CAD", "C:USDCAD" },
        { "USD/CHF", "C:USDCHF" },
        { "XAU/USD", "C:XAUUSD" },
        { "BTC/USD", "X:BTCUSD" },
    };

    /// <summary>Timeframe display name → (Multiplier, Timespan, HistoryDays)</summary>
    public static readonly Dictionary<string, (int Mult, string Span, int Days)> Timeframes = new()
    {
        { "Weekly",     (1,  "week",   730) },
        { "Daily",      (1,  "day",    92)  },
        { "4 Hour",     (4,  "hour",   31)  },
        { "Hourly",     (1,  "hour",   31)  },
        { "15 Minute",  (15, "minute", 5)   },
    };

    public static readonly Dictionary<string, double> PairAtrMultipliers = new()
    {
        { "EUR/USD", 1.5 }, { "GBP/USD", 1.8 }, { "USD/JPY", 1.5 }, { "USD/ZAR", 2.5 },
        { "AUD/USD", 1.5 }, { "NZD/USD", 1.6 }, { "USD/CAD", 1.5 }, { "USD/CHF", 1.5 },
        { "XAU/USD", 2.0 }, { "BTC/USD", 2.0 },
    };

    public static readonly Dictionary<string, double> PairMinStop = new()
    {
        { "EUR/USD", 0.0010 }, { "GBP/USD", 0.0015 }, { "USD/JPY", 0.10 }, { "USD/ZAR", 0.05 },
        { "AUD/USD", 0.0010 }, { "NZD/USD", 0.0010 }, { "USD/CAD", 0.0010 }, { "USD/CHF", 0.0010 },
        { "XAU/USD", 2.00   }, { "BTC/USD", 500.0   },
    };

    public static double PipSize(string pair) => pair switch
    {
        var p when p.Contains("JPY") => 0.01,
        "XAU/USD" => 0.10,
        "BTC/USD" => 1.0,
        var p when p.Contains("ZAR") => 0.001,
        _ => 0.0001,
    };
}

public class AppSettings
{
    public string MassiveApiKey         { get; set; } = "";
    public int    CacheTtlSeconds       { get; set; } = 300;
    public int    RefreshIntervalSeconds { get; set; } = 300;
    public EmailSettings  Email { get; set; } = new();
    public RiskSettings   Risk  { get; set; } = new();
}

public class EmailSettings
{
    public string SmtpHost     { get; set; } = "smtp.gmail.com";
    public int    SmtpPort     { get; set; } = 587;
    public string SmtpUser     { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string Sender       { get; set; } = "";
    public string Recipient    { get; set; } = "";
}

public class RiskSettings
{
    public double RiskPerTrade      { get; set; } = 0.02;
    public double AtrSlMultiplier   { get; set; } = 1.5;
    public double Tp1AtrMultiplier  { get; set; } = 3.0;
    public double Tp2AtrMultiplier  { get; set; } = 5.0;
    public double MinRiskReward     { get; set; } = 2.0;
    public double AdxTrendMin       { get; set; } = 20.0;
    public double RsiOversold       { get; set; } = 40.0;
    public double RsiOverbought     { get; set; } = 60.0;
    public double StochOversold     { get; set; } = 25.0;
    public double StochOverbought   { get; set; } = 75.0;
}