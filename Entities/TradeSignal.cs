namespace Orion.MacroEconomics.Entities;

public class TradeSignal
{
    public string Bias { get; set; }
    public decimal Entry { get; set; }
    public string? Pair { get; set; }
    public decimal TakeProfit1 { get; set; }
    public string RiskReward1 { get; set; }
    public string StopLoss { get; set; }
    public int StrengthScore { get; set; }
    public string Conviction { get; set; }
    public List<string> Thesis { get; set; }
    public int Confidence { get; set; }
    public decimal TakeProfit2 { get; set; }
    public decimal RiskReward2 { get; set; }
    public decimal StopLossMethod { get; set; }
    public decimal StopLossPips { get; set; }
    public DateTime GeneratedAt { get; set; }
    public decimal ATR { get; set; }
}