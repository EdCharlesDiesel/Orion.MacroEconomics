using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.DTO;

public class HighConvictionResponse
{
    public List<TradingIdea> TradingIdeas { get; set; } = new();
    public List<SwingTradingIdea> SwingIdeas { get; set; } = new();
    public int TotalHighConviction { get; set; }
    public DateTime GeneratedAt { get; set; }
}