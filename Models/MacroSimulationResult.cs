using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Models;
public sealed class MacroSimulationResult
{
    public string Direction { get; set; } = "NEUTRAL";
    public decimal Confidence { get; set; }
    public List<MacroState> States { get; set; }
    public MarketRegime FinalRegime { get; set; }
    public double SuccessRate { get; set; }
    public DateTime TimestampUtc { get; set; }
}