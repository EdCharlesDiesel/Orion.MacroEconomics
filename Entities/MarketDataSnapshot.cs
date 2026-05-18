using System.Text.Json;

namespace Orion.MacroEconomics.Entities;
public sealed class MarketDataSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "";
    public string DataType { get; set; } = ""; 
    public string Symbol { get; set; } = "";
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedUtc { get; set; }
}