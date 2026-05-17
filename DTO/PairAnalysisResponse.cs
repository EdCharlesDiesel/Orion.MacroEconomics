using Orion.MacroEconomics.Controllers;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.DTO;

public class PairAnalysisResponse
{
    public string Pair { get; set; } = string.Empty;
    public Dictionary<string, TimeframeAnalysis> Timeframes { get; set; } = new();
    public TradingIdea? TradingIdea { get; set; }
    public SwingTradingIdea? SwingIdea { get; set; }
    public EntrySignalResult? EntrySignal { get; set; }
}