using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Entities;

public class ComponentDetails
{
    public string Name { get; set; } = string.Empty;
    public HealthComponentType Type { get; set; }
    public bool Critical { get; set; }
    public DateTime LastCheck { get; set; }
    public HealthCheckResult? LastResult { get; set; }
    public long TotalChecks { get; set; }
    public long FailedChecks { get; set; }
    public double SuccessRate { get; set; }
}