namespace Orion.MacroEconomics.Entities;

public class ComplianceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateRange Period { get; set; } = new();
    public string? Pair { get; set; }
    public int TotalDecisions { get; set; }
    public Dictionary<string, int> TradesByDirection { get; set; } = new();
    public decimal AverageConfidence { get; set; }
    public Dictionary<DateTime, int> DecisionDistribution { get; set; } = new();
}