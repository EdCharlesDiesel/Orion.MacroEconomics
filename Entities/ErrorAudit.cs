namespace Orion.MacroEconomics.Entities;

public class ErrorAudit
{
    public string Stage { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}
