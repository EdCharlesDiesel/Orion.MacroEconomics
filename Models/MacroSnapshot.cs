namespace Orion.MacroEconomics.Models;

public class MacroSnapshot
{
    public string Currency { get; set; } = string.Empty;  // e.g. "USD", "EUR"
    public decimal GDP { get; set; }                       // e.g. in billions USD
    public decimal Inflation { get; set; }                 // e.g. 3.5 for 3.5%
    public decimal Unemployment { get; set; }              // e.g. 4.1 for 4.1%
    public decimal Rates { get; set; }                     // e.g. 5.25 for 5.25%
}