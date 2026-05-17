namespace Orion.MacroEconomics.Controllers;

public class TimeframeAnalysis
{
    public decimal LatestPrice { get; set; }
    public decimal LatestClose { get; set; }
    public decimal RSI { get; set; }
    public decimal ADX { get; set; }
    public string Trend { get; set; } = string.Empty;
    public int DataPoints { get; set; }
}