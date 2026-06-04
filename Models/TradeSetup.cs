namespace Orion.MacroEconomics.Models;


public class TradeSetup
{
    public int Id { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public string Instrument { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public double Atr14 { get; set; }
    public double Atr20 { get; set; }
    public double SlPips { get; set; }
    public double Tp1Pips { get; set; }
    public double Tp2Pips { get; set; }
    public double LotSize { get; set; }
    public double RiskAmount { get; set; }
    public double RrTp1 { get; set; }
    public double RrTp2 { get; set; }
    public double AccountBalance { get; set; }
    public double RiskPct { get; set; }
    public int ChecksPassed { get; set; }
    public int ChecksTotal { get; set; }
    public string? ChecksDetail { get; set; } // JSON string
    public string? Notes { get; set; }

    // Outcome tracking
    public double? EntryPrice { get; set; }
    public string? Outcome { get; set; }
    public double? ClosePrice { get; set; }
    public double? PipsGained { get; set; }
    public double? RMultiple { get; set; }
    public bool IsOpen { get; set; } = true;
}