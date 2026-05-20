namespace Orion.MacroEconomics.Configurations;

public sealed class LiveTradingConfig
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public Dictionary<string, PairTradingConfig> Pairs { get; set; } = new();
}

public sealed class PairTradingConfig
{
    public bool Enabled { get; set; } = true;
    public decimal? AtrStopMultiplier { get; set; }
    public decimal? MinStopDistance { get; set; }
}