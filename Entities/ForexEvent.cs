using Marten.Schema;

namespace Orion.MacroEconomics.Entities;

public class ForexEvent
{
    [Identity]
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal? Actual { get; set; }
    public decimal? Forecast { get; set; }
    public decimal? Previous { get; set; }
    public string Impact { get; set; } = "Low";
    public DateTime RetrievedAt { get; set; }
    public bool IsProcessed { get; set; }
}

public class ForexNews
{
    [Identity]
    public Guid Id { get; set; }
    public string NewsId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public List<string> AffectedCurrencies { get; set; } = new();
    public int SentimentScore { get; set; } // -100 to 100
    public DateTime RetrievedAt { get; set; }
}