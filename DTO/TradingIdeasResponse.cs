using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

public class TradingIdeasResponse
{
    public List<TradingIdea> TradingIdeas { get; set; } = new();
    public List<SwingTradingIdea> SwingIdeas { get; set; } = new();
    public DateTime? GeneratedAt { get; set; }
    public int TotalPairsAnalyzed { get; set; }
    public List<string> SkippedPairs { get; set; } = new();
    public IdeasSummary Summary { get; set; } = new();
    public string? Message { get; set; }
}