namespace Orion.MacroEconomics.DTO;

public sealed class ProviderDataResult
{
    public string Provider { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string DataType { get; set; } = "";
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<object> Payload { get; set; } = new List<object>(); 
}