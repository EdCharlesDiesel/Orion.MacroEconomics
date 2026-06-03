namespace Orion.MacroEconomics.Entities;

public class DataQualityReport
{
    public string Pair { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DataQualityResult Quality { get; set; } = new();
    public int DataPoints { get; set; }
    public DateRange DateRange { get; set; } = new();
}