using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Configuration
{
    public class AppConfiguration
    {
        // Version and Cache Settings
        public string Version { get; set; } = "1.0.0";
        public int CacheTTLSeconds { get; set; } = 300;
        public int AutoRefreshIntervalSeconds { get; set; } = 300;

        // Risk Management
        public decimal RiskPerTrade { get; set; } = 0.02m;
        public decimal ATRSLMult { get; set; } = 2.0m;
        public decimal TP1ATRMult { get; set; } = 3.0m;
        public decimal TP2ATRMult { get; set; } = 5.0m;
        public decimal MinRR { get; set; } = 2.0m;

        // Stop Loss Settings
        public decimal DefaultMinStop { get; set; } = 0.0010m;
        public decimal StopBufferPercent { get; set; } = 0.25m;

        // Indicator Thresholds
        public decimal ADXTrendMin { get; set; } = 20.0m;
        public decimal RSI_OS { get; set; } = 40.0m;
        public decimal RSI_OB { get; set; } = 60.0m;
        public decimal StochOS { get; set; } = 25.0m;
        public decimal StochOB { get; set; } = 75.0m;

        // Fix: Proper type for TradingSystem
        public LiveTradingConfig? TradingSystem { get; set; }

        // Remove or fix these redundant properties
        // public object TradingSystem { get; set; } // Remove this line
        // public decimal DefaultMinStopDistance { get; set; } // Either keep this or DefaultMinStop, not both

        public Dictionary<string, decimal> PairATRMultipliers { get; set; } = new()
        {
            ["EURUSD"] = 1.8m,
            ["GBPUSD"] = 2.0m,
            ["USDJPY"] = 1.2m,
            ["BTCUSD"] = 3.0m,
            ["ETHUSD"] = 3.5m
        };

        public Dictionary<string, decimal> PairMinStop { get; set; } = new()
        {
            ["EURUSD"] = 0.0008m,
            ["GBPUSD"] = 0.0010m,
            ["USDJPY"] = 0.080m,
            ["BTCUSD"] = 50.0m,
            ["ETHUSD"] = 5.0m
        };

        public Dictionary<string, string> Assets { get; set; } = new()
        {
            ["EUR/USD"] = "EURUSD=X",
            ["GBP/USD"] = "GBPUSD=X",
            ["USD/JPY"] = "JPY=X",
            ["USD/ZAR"] = "ZAR=X",
            ["AUD/USD"] = "AUDUSD=X",
            ["NZD/USD"] = "NZDUSD=X",
            ["USD/CAD"] = "CAD=X",
            ["USD/CHF"] = "CHF=X",
            ["XAU/USD"] = "GC=F",
            ["BTC/USD"] = "BTC-USD"
        };

        public Dictionary<string, TimeframeConfig> Timeframes { get; set; } = new()
        {
            ["Weekly"] = new() { Interval = "1wk", Period = "3mo" },
            ["Daily"] = new() { Interval = "1d", Period = "3mo" },
            ["4 Hour"] = new() { Interval = "1h", Period = "1mo" },
            ["Hourly"] = new() { Interval = "1h", Period = "1mo" },
            ["15 Minute"] = new() { Interval = "15m", Period = "5d" }
        };

        // These should not have 'internal set' unless you have a specific reason
        public bool UseMockData { get; set; }
        public object? ApiBaseUrl { get; set; }
        public decimal DefaultMinStopDistance { get; set; }
    }

    // Fix LiveTradingConfig to have proper structure
    public class LiveTradingConfig
    {
        public Dictionary<string, PairTradingConfig> Pairs { get; set; } = new();
        // Add other live trading specific properties
        public bool EnableLiveTrading { get; set; }
        public string? ApiKey { get; set; }
        // etc.
    }

    public class PairTradingConfig
    {
        // This is confusing - you have Pairs dictionary inside PairTradingConfig?
        // It should probably be something like this:
        public int AtrStopMultiplier { get; set; }
        public int MinStopDistance { get; set; }
        public decimal? CustomMinStop { get; set; }
        public bool Enabled { get; set; } = true;
        // Add other pair-specific configuration
    }
}