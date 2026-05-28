namespace Orion.MacroEconomics.Models;

public class StrategySignal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Symbol { get; set; }
    public string Direction { get; set; } // LONG or SHORT
    public decimal Entry { get; set; }
    public decimal StopLoss { get; set; }
    public List<decimal> TakeProfits { get; set; } = new();
    public int Confidence { get; set; }
    public Dictionary<int, decimal> SmaValues { get; set; } = new();
    public PivotLevels PivotLevels { get; set; }
    public DateTime SignalTime { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, EXECUTED, REJECTED, EXPIRED
    public string RejectionReason { get; set; }
}

public class PivotLevels
{
    public decimal PP { get; set; }
    public decimal R1 { get; set; }
    public decimal R2 { get; set; }
    public decimal R3 { get; set; }
    public decimal S1 { get; set; }
    public decimal S2 { get; set; }
    public decimal S3 { get; set; }
}

public class TradeExecution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SignalId { get; set; }
    public string Symbol { get; set; }
    public string Direction { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal PositionSize { get; set; }
    public decimal StopLoss { get; set; }
    public Dictionary<string, decimal> TakeProfits { get; set; } = new();
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? PnL { get; set; }
    public decimal? PnLPercent { get; set; }
    public string Status { get; set; } = "OPEN";
}

public class DailyStrategyState
{
    public DateTime Date { get; set; }
    public int TradesExecuted { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal PeakEquity { get; set; }
    public decimal Drawdown { get; set; }
    public bool MaxLossHit { get; set; }
    public Dictionary<string, List<StrategySignal>> Signals { get; set; } = new();
}