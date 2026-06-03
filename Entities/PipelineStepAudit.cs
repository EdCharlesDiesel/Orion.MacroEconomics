namespace Orion.MacroEconomics.Entities;

public class PipelineStepAudit
{
    public string StepName { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public string DataType { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
