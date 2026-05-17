namespace Orion.MacroEconomics.Entities;

public sealed class MarketDataDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "AlphaVantage";
    public string Pair { get; set; } = "";
    public string DataType { get; set; } = "FX_DAILY";
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public object Payload { get; set; } = new();
}

public sealed class TradingSignalDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Pair { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal FastSma { get; set; }
    public decimal SlowSma { get; set; }
    public decimal LastClose { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public decimal PivotPoint { get; set; }
    public decimal Support1 { get; set; }
    public decimal Support2 { get; set; }
    public decimal Resistance1 { get; set; }
    public decimal Resistance2 { get; set; }
}