namespace Orion.MacroEconomics.DTO;

public class ForexEventDto
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime? Actual { get; set; }
    public decimal? Forecast { get; set; }
    public decimal? Previous { get; set; }
    public string Impact { get; set; } = string.Empty;
    public bool IsUpcoming => EventDate > DateTime.UtcNow;
}

public class CreateForexEventDto
{
    public string Currency { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime? Actual { get; set; }
    public decimal? Forecast { get; set; }
    public decimal? Previous { get; set; }
    public string Impact { get; set; } = "Low";
}

public class ForexNewsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public List<string> AffectedCurrencies { get; set; } = new();
    public int SentimentScore { get; set; }
}