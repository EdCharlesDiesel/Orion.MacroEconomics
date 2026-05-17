using System.Text.Json.Serialization;

namespace Orion.MacroEconomics.Entities;


/// <summary>
/// Represents a single news article returned by the Trading Economics API.
/// </summary>
public sealed class TradingEconomicsNewsItem
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("date")]
    public DateTime Date { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("importance")]
    public int Importance { get; init; }
}

/// <summary>
/// Represents a single intraday OHLC bar from the Trading Economics Markets API.
/// </summary>
public sealed class TradingEconomicsIntradayBar
{
    [JsonPropertyName("Symbol")]
    public string? Symbol { get; init; }

    [JsonPropertyName("Date")]
    public DateTime Date { get; init; }

    [JsonPropertyName("Open")]
    public decimal Open { get; init; }

    [JsonPropertyName("High")]
    public decimal High { get; init; }

    [JsonPropertyName("Low")]
    public decimal Low { get; init; }

    [JsonPropertyName("Close")]
    public decimal Close { get; init; }
}