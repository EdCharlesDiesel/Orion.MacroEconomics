namespace Orion.MacroEconomics.Entities;

public class DataSeries
{
    public string Symbol { get; set; } = string.Empty;
    public List<decimal> Values { get; set; } = new();
}
