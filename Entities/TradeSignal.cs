namespace Orion.MacroEconomics.Entities;

public class TradeSignal
{
    public string?  Pair          { get; set; }
    public string   Bias          { get; set; } = string.Empty;
    public string   Conviction    { get; set; } = string.Empty;
    public int      StrengthScore { get; set; }
    public int      Confidence    { get; set; }
    public string   Thesis        { get; set; } = string.Empty;

    public decimal Entry        { get; set; }
    public decimal TakeProfit1  { get; set; }
    public decimal TakeProfit2  { get; set; }
    public decimal StopLoss     { get; set; }

    public decimal RiskReward1  { get; set; }
    public decimal RiskReward2  { get; set; }

    public string  StopLossMethod { get; set; } = string.Empty;
    public decimal StopLossPips   { get; set; }

    public decimal  ATR         { get; set; }
    public DateTime GeneratedAt { get; set; }
}
