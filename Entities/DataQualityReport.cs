namespace Orion.MacroEconomics.Entities;

public class DataQualityReport
{
    public string Pair { get; set; }
    public DateTime Timestamp { get; set; }
    public DataQualityResult Quality { get; set; }
    public int DataPoints { get; set; }
    public DateRange DateRange { get; set; }
}