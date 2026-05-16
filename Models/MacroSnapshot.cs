namespace Orion.MacroEconomics.Models;

public class MacroSnapshot
{
    public object Currency { get; set; }
    public object GDP { get; set; }
    public object Inflation { get; set; }
    public decimal Unemployment { get; set; }
    public decimal Rates { get; set; }
}