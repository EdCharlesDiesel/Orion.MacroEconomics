namespace Orion.MacroEconomics.Entities;

public class EventAudit
{
    public string EventName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
